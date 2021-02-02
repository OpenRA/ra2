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
# to check the engine and your mod dlls for StyleCop violations, run:
#   make check
#
# the following are internal sdk helpers that are not intended to be run directly:
#   make check-variables
#   make check-sdk-scripts
#   make check-packaging-scripts

.PHONY: check-sdk-scripts check-packaging-scripts check-variables engine all clean version check-scripts check test
.DEFAULT_GOAL := all

PYTHON = $(shell command -v python3 2> /dev/null)
ifeq ($(PYTHON),)
PYTHON = $(shell command -v python 2> /dev/null)
endif
ifeq ($(PYTHON),)
$(error "The OpenRA mod SDK requires python.")
endif

VERSION = $(shell git name-rev --name-only --tags --no-undefined HEAD 2>/dev/null || echo git-`git rev-parse --short HEAD`)
MOD_ID = $(shell cat user.config mod.config 2> /dev/null | awk -F= '/MOD_ID/ { print $$2; exit }')
ENGINE_DIRECTORY = $(shell cat user.config mod.config 2> /dev/null | awk -F= '/ENGINE_DIRECTORY/ { print $$2; exit }')
MOD_SEARCH_PATHS = "$(shell $(PYTHON) -c "import os; print(os.path.realpath('.'))")/mods,./mods"

WHITELISTED_OPENRA_ASSEMBLIES = "$(shell cat user.config mod.config 2> /dev/null | awk -F= '/WHITELISTED_OPENRA_ASSEMBLIES/ { print $$2; exit }')"
WHITELISTED_THIRDPARTY_ASSEMBLIES = "$(shell cat user.config mod.config 2> /dev/null | awk -F= '/WHITELISTED_THIRDPARTY_ASSEMBLIES/ { print $$2; exit }')"
WHITELISTED_CORE_ASSEMBLIES = "$(shell cat user.config mod.config 2> /dev/null | awk -F= '/WHITELISTED_CORE_ASSEMBLIES/ { print $$2; exit }')"
WHITELISTED_MOD_ASSEMBLIES = "$(shell cat user.config mod.config 2> /dev/null | awk -F= '/WHITELISTED_MOD_ASSEMBLIES/ { print $$2; exit }')"

MANIFEST_PATH = "mods/$(MOD_ID)/mod.yaml"
HAS_LUAC = $(shell command -v luac 2> /dev/null)
LUA_FILES = $(shell find mods/*/maps/* -iname '*.lua' 2> /dev/null)
MOD_SOLUTION_FILES = $(shell find . -maxdepth 1 -iname '*.sln' 2> /dev/null)

MSBUILD = msbuild -verbosity:m -nologo

ifndef TARGETPLATFORM
UNAME_S := $(shell uname -s)
UNAME_M := $(shell uname -m)
ifeq ($(UNAME_S),Darwin)
TARGETPLATFORM = osx-x64
else
ifeq ($(UNAME_M),x86_64)
TARGETPLATFORM = linux-x64
else
TARGETPLATFORM = unix-generic
endif
endif
endif

check-sdk-scripts:
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

check-packaging-scripts:
	@if [ ! -x "packaging/package-all.sh" ] || [ ! -x "packaging/linux/buildpackage.sh" ] || [ ! -x "packaging/macos/buildpackage.sh" ] || [ ! -x "packaging/windows/buildpackage.sh" ]; then \
		echo "Required SDK scripts are not executable:"; \
		if [ ! -x "packaging/package-all.sh" ]; then \
			echo "   packaging/package-all.sh"; \
		fi; \
		if [ ! -x "packaging/linux/buildpackage.sh" ]; then \
			echo "   packaging/linux/buildpackage.sh"; \
		fi; \
		if [ ! -x "packaging/macos/buildpackage.sh" ]; then \
			echo "   packaging/macos/buildpackage.sh"; \
		fi; \
		if [ ! -x "packaging/windows/buildpackage.sh" ]; then \
			echo "   packaging/windows/buildpackage.sh"; \
		fi; \
		echo "Repair their permissions and try again."; \
		echo "If you are using git you can repair these permissions by running"; \
		echo "   git update-index --chmod=+x *.sh"; \
		echo "in the directories containing the affected files"; \
		echo "and commiting the changed files to your repository."; \
		exit 1; \
	fi

check-variables:
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

engine: check-variables check-sdk-scripts
	@./fetch-engine.sh || (printf "Unable to continue without engine files\n"; exit 1)
	@cd $(ENGINE_DIRECTORY) && make TARGETPLATFORM=$(TARGETPLATFORM) all

all: engine
	@command -v $(MSBUILD) >/dev/null || (echo "OpenRA requires the '$(MSBUILD)' tool provided by Mono >= 5.18."; exit 1)
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec $(MSBUILD) -t:Build -restore -p:Configuration=Release -p:TargetPlatform=$(TARGETPLATFORM) \;
endif

clean: engine
	@command -v $(MSBUILD) >/dev/null || (echo "OpenRA requires the '$(MSBUILD)' tool provided by Mono >= 5.18."; exit 1)
ifneq ("$(MOD_SOLUTION_FILES)","")
	@find . -maxdepth 1 -name '*.sln' -exec $(MSBUILD) -t:clean \;
endif
	@cd $(ENGINE_DIRECTORY) && make clean

version: check-variables
	@sh -c '. $(ENGINE_DIRECTORY)/packaging/functions.sh; set_mod_version $(VERSION) $(MANIFEST_PATH)'
	@printf "Version changed to $(VERSION).\n"

check-scripts: check-variables
ifeq ("$(HAS_LUAC)","")
	@printf "'luac' not found.\n" && exit 1
endif
	@echo
	@echo "Checking for Lua syntax errors..."
ifneq ("$(LUA_FILES)","")
	@luac -p $(LUA_FILES)
endif

check: engine
ifneq ("$(MOD_SOLUTION_FILES)","")
	@echo "Compiling in debug mode..."
	@find . -maxdepth 1 -name '*.sln' -exec $(MSBUILD) -t:Build -restore -p:Configuration=Debug -p:TargetPlatform=$(TARGETPLATFORM) \;
endif
	@echo "Checking runtime assemblies..."
	@./utility.sh --check-runtime-assemblies $(WHITELISTED_OPENRA_ASSEMBLIES) $(WHITELISTED_THIRDPARTY_ASSEMBLIES) $(WHITELISTED_CORE_ASSEMBLIES) $(WHITELISTED_MOD_ASSEMBLIES)
	@echo "Checking for explicit interface violations..."
	@./utility.sh --check-explicit-interfaces
	@echo "Checking for incorrect conditional trait interface overrides..."
	@./utility.sh --check-conditional-trait-interface-overrides

test: all
	@echo "Testing $(MOD_ID) mod MiniYAML..."
	@./utility.sh --check-yaml
