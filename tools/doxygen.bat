IF NOT %1 == Release GOTO end

cd "%2tools"

doxygen.exe %3 > ../doc/doxygen.log

:end
