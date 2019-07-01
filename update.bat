:; git submodule foreach 'git fetch origin; git checkout $(git describe --tags `git rev-list --tags --max-count=1`);'; exit $?
@ECHO OFF
"C:\Program Files\Git\bin\bash.exe" -c "git submodule foreach 'git fetch origin; git checkout $(git describe --tags `git rev-list --tags --max-count=1`);'"