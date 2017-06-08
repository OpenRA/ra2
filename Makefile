############################# INSTRUCTIONS #############################
#
# to compile, run:
#   make
#
# to remove the files created by compiling, run:
#   make clean
#
# to set the mods version, run:
#   make version [VERSION="custom-version"]
#
# to check lua scripts for syntax errors, run:
#   make check-scripts
#
# to check the official mods for erroneous yaml files, run:
#   make test
#
# to check the official mod dlls for StyleCop violations, run:
#   make check
#

.PHONY: utility stylecheck build clean engine version check-scripts check check-sequence-sprites test
.DEFAULT_GOAL := build

VERSION = $(shell git name-rev --name-only --tags --no-undefined HEAD 2>/dev/null || echo git-`git rev-parse --short HEAD`)
MOD_ID = $(shell awk -F= '/MOD_ID/ { print $$2 }' mod.config)
ENGINE_DIRECTORY = $(shell awk -F= '/ENGINE_DIRECTORY/ { print $$2 }' mod.config)
MOD_SLN_DIRECTORY = $(shell awk -F= '/MOD_SLN_DIRECTORY/ { print $$2 }' mod.config)
AUTOMATIC_ENGINE_MANAGEMENT = $(shell awk -F= '/AUTOMATIC_ENGINE_MANAGEMENT/ { print $$2 }' mod.config)

INCLUDE_DEFAULT_MODS = $(shell awk -F= '/INCLUDE_DEFAULT_MODS/ { print $$2 }' mod.config)

MOD_SEARCH_PATHS = "$(shell python -c "import os; print(os.path.realpath('.'))")/mods"
ifeq ($(INCLUDE_DEFAULT_MODS),"True")
	MOD_SEARCH_PATHS := "$(MOD_SEARCH_PATHS),./mods"
endif

MANIFEST_PATH = "mods/$(MOD_ID)/mod.yaml"

HAS_MSBUILD = $(shell command -v msbuild 2> /dev/null)
HAS_LUAC = $(shell command -v luac 2> /dev/null)
LUA_FILES = $(shell find mods/*/maps/* -iname '*.lua')

engine:
	@./fetch-engine.sh || (printf "Unable to continue without engine files\n"; exit 1)
	@cd $(ENGINE_DIRECTORY) && make core

utility: engine
	@test -f "$(ENGINE_DIRECTORY)/OpenRA.Utility.exe" || (printf "OpenRA.Utility.exe not found!\n"; exit 1)

stylecheck: engine
	@test -f "$(ENGINE_DIRECTORY)/OpenRA.StyleCheck.exe" || (cd $(ENGINE_DIRECTORY) && make stylecheck)

build: engine
ifeq ("$(HAS_MSBUILD)","")
	@cd $(MOD_SLN_DIRECTORY) && find . -maxdepth 1 -name '*.sln' -exec xbuild /nologo /verbosity:quiet /p:TreatWarningsAsErrors=true \;
else
	@cd $(MOD_SLN_DIRECTORY) && find . -maxdepth 1 -name '*.sln' -exec msbuild /t:Rebuild /nr:false \;
endif
	@cd $(MOD_SLN_DIRECTORY) && find . -maxdepth 1 -name '*.sln' -exec printf "The mod logic has been built.\n" \;

clean: engine
ifeq ("$(HAS_MSBUILD)","")
	@cd $(MOD_SLN_DIRECTORY) && find . -maxdepth 1 -name '*.sln' -exec xbuild /nologo /verbosity:quiet /p:TreatWarningsAsErrors=true /t:Clean \;
else
	@cd $(MOD_SLN_DIRECTORY) && find . -maxdepth 1 -name '*.sln' -exec msbuild /t:Clean /nr:false \;
endif
	@cd $(MOD_SLN_DIRECTORY) && find . -maxdepth 1 -name '*.sln' -exec printf "The mod logic has been cleaned.\n" \;
	@cd $(ENGINE_DIRECTORY) && make clean
	@printf "The engine has been cleaned.\n"

version:
	@awk '{sub("Version:.*$$","Version: $(VERSION)"); print $0}' $(MANIFEST_PATH) > $(MANIFEST_PATH).tmp && \
	awk '{sub("/[^/]*: User$$", "/$(VERSION): User"); print $0}' $(MANIFEST_PATH).tmp > $(MANIFEST_PATH) && \
	rm $(MANIFEST_PATH).tmp
	@printf "Version changed to $(VERSION).\n"

check-scripts:
ifeq ("$(HAS_LUAC)","")
	@printf "'luac' not found.\n" && exit 1
endif
	@echo
	@echo "Checking for Lua syntax errors..."
ifneq ("$(LUA_FILES)","")
	@luac -p $(LUA_FILES)
endif

check: utility stylecheck
	@echo "Checking for explicit interface violations..."
	@MOD_SEARCH_PATHS="$(MOD_SEARCH_PATHS)" mono --debug "$(ENGINE_DIRECTORY)/OpenRA.Utility.exe" $(MOD_ID) --check-explicit-interfaces
	@echo "Checking for code style violations in OpenRA.Mods.$(MOD_ID)..."
	@mono --debug "$(ENGINE_DIRECTORY)/OpenRA.StyleCheck.exe" OpenRA.Mods.$(MOD_ID)

check-sequence-sprites: utility
	@echo
	@echo "Checking $(MOD_ID) sprite sequences..."
	@MOD_SEARCH_PATHS="${MOD_SEARCH_PATHS}" mono --debug engine/OpenRA.Utility.exe $(MOD_ID) --check-sequence-sprites

test: utility
	@echo "Testing $(MOD_ID) mod MiniYAML..."
	@MOD_SEARCH_PATHS="$(MOD_SEARCH_PATHS)" mono --debug "$(ENGINE_DIRECTORY)/OpenRA.Utility.exe" $(MOD_ID) --check-yaml