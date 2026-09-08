@echo off
GOTO endcommentblock
:: ---------------------------------------------------------------------------
::
::  Copyright 2026 NOpenNLP Contributors
::
::  Licensed under the Apache License, Version 2.0 (the "License");
::  you may not use this file except in compliance with the License.
::  You may obtain a copy of the License at
::
::      http://www.apache.org/licenses/LICENSE-2.0
::
::  Unless required by applicable law or agreed to in writing, software
::  distributed under the License is distributed on an "AS IS" BASIS,
::  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
::  See the License for the specific language governing permissions and
::  limitations under the License.
::
:: Thin wrapper that forwards to run-compat.ps1, which holds all the logic. See
:: that script for what the harness does (both directions of the Apache OpenNLP
:: <-> NOpenNLP model compatibility check, issue #46) and the -TargetFramework
:: parameter.
::
:: Arguments are forwarded, e.g.:
::
::     run-compat.bat -TargetFramework net8.0
::
:: ---------------------------------------------------------------------------
:endcommentblock
where pwsh >nul 2>nul
:: -File (not -Command) so that arguments such as -TargetFramework reach the
:: script's param block; with -Command, trailing arguments are dropped.
if %ERRORLEVEL% NEQ 0 (echo "PowerShell Core (pwsh) could not be found. Please install version 7 or later." & exit /b 1) else (pwsh -ExecutionPolicy bypass -File "%~dpn0.ps1" %*)
