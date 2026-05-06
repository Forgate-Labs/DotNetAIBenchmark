#!/usr/bin/env bash
set -euo pipefail

bash hidden-tests/HiddenTests/evaluate.sh chat-behavior
bash hidden-tests/HiddenTests/evaluate.sh mvvm
bash hidden-tests/HiddenTests/evaluate.sh solid
bash hidden-tests/HiddenTests/evaluate.sh security
bash hidden-tests/HiddenTests/evaluate.sh docker
bash hidden-tests/HiddenTests/evaluate.sh readme
