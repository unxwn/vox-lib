#!/usr/bin/env bash
# Applies branch protection to `main`: require a PR, require CI green, block
# force-pushes and deletion.
#
# Run once, after the GitHub repo exists and `gh auth login` has been done:
#     bash .github/setup-branch-protection.sh
#
# IMPORTANT: on a personal account's PRIVATE repo on the GitHub Free plan,
# enforced protection may not be available (it has historically required Pro).
# This script tells you plainly if GitHub refuses, rather than pretending it
# worked. If it is refused you have three options:
#   1. make the repo public (protection is free on public repos), or
#   2. upgrade the account to GitHub Pro, or
#   3. keep working via PRs by discipline; CI still runs and still reports on
#      every PR, the checks simply are not *enforced* as required.
set -euo pipefail

REPO="$(gh repo view --json nameWithOwner -q .nameWithOwner)"
echo "Target repository: $REPO"

# Job names from .github/workflows/ci.yml become the status check contexts.
CHECK_BACKEND='backend (build + test)'
CHECK_FRONTEND='frontend (lint + build)'

payload() {
  cat <<JSON
{
  "name": "protect main",
  "target": "branch",
  "enforcement": "active",
  "conditions": { "ref_name": { "include": ["~DEFAULT_BRANCH"], "exclude": [] } },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 0,
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_review_thread_resolution": true
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "required_status_checks": [
          { "context": "$CHECK_BACKEND" },
          { "context": "$CHECK_FRONTEND" }
        ]
      }
    }
  ]
}
JSON
}

# required_approving_review_count is 0 on purpose: GitHub does not let you
# approve your own pull request, so requiring 1 approval on a solo project
# would make every PR permanently unmergeable.

echo "Applying ruleset..."
if payload | gh api "repos/$REPO/rulesets" -X POST --input - >/dev/null 2>/tmp/ruleset-err; then
  echo "OK: 'protect main' ruleset applied."
  echo "     main now requires a PR with both CI jobs green, and rejects force-push/deletion."
else
  echo
  echo "GitHub refused the ruleset. Response:"
  sed 's/^/    /' /tmp/ruleset-err
  echo
  echo "Most likely this plan does not allow enforced protection on a private"
  echo "repository. See the header of this script for the three options."
  exit 1
fi
