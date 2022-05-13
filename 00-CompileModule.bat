@echo off

call MC7D2D PinRecipes.dll ^
/reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" ^
Harmony\*.cs Dependency\*.cs && ^
echo Successfully compiled PinRecipes.dll

pause