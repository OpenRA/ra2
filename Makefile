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
MOD_ID = $(shell cat user.config mod.config 2> /dev/null | awk -F= '/MOD_ID/ { print $$2; exit }')
ENGINE_DIRECTORY = $(shell cat user.config mod.config 2> /dev/null | awk -F= '/ENGINE_DIRECTORY/ { print $$2; exit }')
MOD_SEARCH_PATHS = "$(shell python -c "import os; print(os.path.realpath('.'))")/mods,./mods"

MANIFEST_PATH = "mods/$(MOD_ID)/mod.yaml"

HAS_MSBUILD = $(shell command -v msbuild 2> /dev/null)
HAS_LUAC = $(shell command -v luac 2> /dev/null)
LUA_FILES = $(shell find mods/*/maps/* -iname '*.lua')
PROJECT_DIRS = $(shell dirname $$(find . -iname "*.csproj" -not -path "$(ENGINE_DIRECTORY)/*"))

scripts:
	@awk '/\r$$/ { exit(1); }' mod.config || (printf "Invalid mod.config format: file must be saved using unix-style (CR, not CRLF) line endings.\n"; exit 1)
	@if [ ! -x "fetch-engine.sh" ] || [ ! -x "launch-dedicated.sh" ] || [ ! -x "launch-game.sh" ] || [ ! -x "utility.sh" ]; then \
		echo "Required SDK scripts are not executable:"; \
		if [ ! -x "fetch-engine.sh" ]; then \
			echo "   fetch-engine.sh"; \
		fi; \
		if [ ! -x "launch-dedicated.sh" ]; then \
			echo "   launch-dedicated.sh"; \
		fi; \
		if [ ! -x "launch-game.sh" ]; then \
			echo "   launch-game.sh"; \
		fi; \
		if [ ! -x "utility.sh" ]; then \
			echo "   utility.sh"; \
		fi; \
		echo "Repair their permissions and try again."; \
		echo "If you are using git you can repair these permissions by running"; \
		echo "   git update-index --chmod=+x *.sh"; \
		echo "and commiting the changed files to your repository."; \
		exit 1; \
	fi

variables:
	@if [ -z "$(MOD_ID)" ] || [ -z "$(ENGINE_DIRECTORY)" ]; then \
		echo "Required mod.config variables are missing:"; \
		if [ -z "$(MOD_ID)" ]; then \
			echo "   MOD_ID"; \
		fi; \
		if [ -z "$(ENGINE_DIRECTORY)" ]; then \
			echo "   ENGINE_DIRECTORY"; \
		fi; \
		echo "Repair your mod.config (or user.config) and try again."; \
		exit 1; \
	fi

engine: variables scripts
	@./fetch-engine.sh || (printf "Unable to continue without engine files\n"; exit 1)
	@cd $(ENGINE_DIRECTORY) && make core

utility: engine
	@test -f "$(ENGINE_DIRECTORY)/OpenRA.Utility.exe" || (printf "OpenRA.Utility.exe not found!\n"; exit 1)

stylecheck: engine
	@test -f "$(ENGINE_DIRECTORY)/OpenRA.StyleCheck.exe" || (cd $(ENGINE_DIRECTORY) && make stylecheck)

build: engine
ifeq ("$(HAS_MSBUILD)","")
	@find . -maxdepth 1 -name '*.sln' -exec xbuild /nologo /verbosity:quiet /p:TreatWarningsAsErrors=true \;
else
	@find . -maxdepth 1 -name '*.sln' -exec msbuild /t:Rebuild /nr:false \;
endif
	@find . -maxdepth 1 -name '*.sln' -exec printf "The mod logic has been built.\n" \;

clean: engine
ifeq ("$(HAS_MSBUILD)","")
	@find . -maxdepth 1 -name '*.sln' -exec xbuild /nologo /verbosity:quiet /p:TreatWarningsAsErrors=true /t:Clean \;
else
	@find . -maxdepth 1 -name '*.sln' -exec msbuild /t:Clean /nr:false \;
endif
	@find . -maxdepth 1 -name '*.sln' -exec printf "The mod logic has been cleaned.\n" \;
	@cd $(ENGINE_DIRECTORY) && make clean
	@printf "The engine has been cleaned.\n"

version: variables
	@awk '{sub("Version:.*$$","Version: $(VERSION)"); print $0}' $(MANIFEST_PATH) > $(MANIFEST_PATH).tmp && \
	awk '{sub("/[^/]*: User$$", "/$(VERSION): User"); print $0}' $(MANIFEST_PATH).tmp > $(MANIFEST_PATH) && \
	rm $(MANIFEST_PATH).tmp
	@printf "Version changed to $(VERSION).\n"

check-scripts: variables
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
	@for i in $(PROJECT_DIRS) ; do \
		echo "Checking for code style violations in $${i}...";\
		mono --debug "$(ENGINE_DIRECTORY)/OpenRA.StyleCheck.exe" $${i};\
	done

check-sequence-sprites: utility
	@echo
	@echo "Checking $(MOD_ID) sprite sequences..."
	@MOD_SEARCH_PATHS="${MOD_SEARCH_PATHS}" mono --debug engine/OpenRA.Utility.exe $(MOD_ID) --check-sequence-sprites

test: utility
	@echo "Testing $(MOD_ID) mod MiniYAML..."
	@MOD_SEARCH_PATHS="$(MOD_SEARCH_PATHS)" mono --debug "$(ENGINE_DIRECTORY)/OpenRA.Utility.exe" $(MOD_ID) --check-yaml