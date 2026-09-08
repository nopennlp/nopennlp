#!/usr/bin/env bash
# Copyright 2026 NOpenNLP Contributors
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Thin wrapper that forwards to run-compat.ps1, which holds all the logic. This
# keeps the cross-platform behaviour in one place; the .sh, .bat and .ps1 entry
# points differ only in how they locate and invoke PowerShell.
#
# See run-compat.ps1 for what the harness does (both directions of the Apache
# OpenNLP <-> NOpenNLP model compatibility check, issue #46) and the
# -TargetFramework parameter.
#
# Arguments are forwarded, e.g.:
#
#   ./run-compat.sh -TargetFramework net8.0

if ! command -v pwsh &> /dev/null
then
    echo "PowerShell Core (pwsh) could not be found. Please install version 7 or later."
    exit 1
fi

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# -File (not -Command) so that arguments such as -TargetFramework reach the
# script's param block; with -Command, trailing arguments are dropped.
pwsh -ExecutionPolicy bypass -File "$HERE/run-compat.ps1" "$@"
