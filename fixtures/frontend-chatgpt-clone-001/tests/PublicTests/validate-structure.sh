#!/usr/bin/env bash
set -euo pipefail

bash tests/PublicTests/evaluate.sh root-layout
bash tests/PublicTests/evaluate.sh build
bash tests/PublicTests/evaluate.sh chat-ui-contract
bash tests/PublicTests/evaluate.sh tailwind
bash tests/PublicTests/evaluate.sh component-tests
bash tests/PublicTests/evaluate.sh vulnerable-packages
