MOD_DIR = mod
ORA_DIR = OpenRA
PACKAGE_DIR = package
INSTALL_DIR = $(HOME)/.openra/mods

# Version of the mod and its content package
VERSION_CMD = git name-rev --name-only --tags --no-undefined HEAD 2>/dev/null || echo git-`git rev-parse --short HEAD`
VERSION = $(shell $(VERSION_CMD))
ORA_VERSION = $(shell cd $(ORA_DIR) && $(VERSION_CMD))

LIB_sources := $(shell find OpenRA.Mods.RA2 -iname ".*" -prune -o -iname '*.cs' -print0)
LIB_assembly = OpenRA.Mods.RA2.dll

ORAMOD_PKG = ra2.oramod
MOD_CONTENTS := $(shell cd $(MOD_DIR) && git ls-files -c -o --exclude-standard)
COPY_CONTENTS = $(filter-out mod.yaml,$(MOD_CONTENTS))
ORAMOD_CONTENTS = $(MOD_CONTENTS) $(LIB_assembly)

ZIP_R9 = zip -r -9 -q
MKDIR_P = mkdir -p
RM_F = rm -f

$(LIB_assembly): $(LIB_sources)
	@xbuild /nologo /property:SolutionDir=$(CURDIR) /property:Platform=x86 $(if $(VERBOSE),,/verbosity:quiet) OpenRA.Mods.RA2/OpenRA.Mods.RA2.csproj

$(PACKAGE_DIR):
	@$(MKDIR_P) $(PACKAGE_DIR)

dependencies:
	$(MAKE) -C $(ORA_DIR) dependencies

lib: $(LIB_assembly)

all: dependencies oramod

copy-pkg: $(PACKAGE_DIR)
	@cd $(MOD_DIR) && cp --parents -u $(COPY_CONTENTS) $(shell realpath $(PACKAGE_DIR))

$(PACKAGE_DIR)/$(LIB_assembly): $(LIB_assembly) $(PACKAGE_DIR)
	@cp $(LIB_assembly) $(PACKAGE_DIR)

$(PACKAGE_DIR)/mod.yaml: $(MOD_DIR)/mod.yaml $(PACKAGE_DIR)
	@sed -e 's/{DEV_VERSION}/$(VERSION)/' \
	     -e 's/{ORA_VERSION}/$(ORA_VERSION)/' \
		$(MOD_DIR)/mod.yaml > $(PACKAGE_DIR)/mod.yaml

$(PACKAGE_DIR)/% : $(MOD_DIR)/% copy-pkg
	@true

$(ORAMOD_PKG): $(addprefix $(PACKAGE_DIR)/,$(ORAMOD_CONTENTS))
	@$(ZIP_R9) $(ORAMOD_PKG) $(PACKAGE_DIR)

oramod: $(ORAMOD_PKG)

clean:
	@$(RM_F) *.dll *.exe *.mdb

distclean: clean
	@$(RM_F) -r $(PACKAGE_DIR)
	@$(RM_F) $(ORAMOD_PKG)

$(INSTALL_DIR):
	$(MKDIR_P) $(INSTALL_DIR)

$(INSTALL_DIR)/$(ORAMOD_PKG): $(ORAMOD_PKG) $(INSTALL_DIR)
	cp $(ORAMOD_PKG) $(INSTALL_DIR)

install: $(INSTALL_DIR)/$(ORAMOD_PKG)

uninstall:
	$(RM_F) $(INSTALL_DIR)/$(ORAMOD_PKG)

.PHONY: version lib all oramod copy-pkg clean distclean dependencies install uninstall

.DEFAULT_GOAL = all
