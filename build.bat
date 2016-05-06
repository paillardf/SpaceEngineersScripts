echo /*** Ignore minified library code below ***/ >> EasyAPI.min.cs
CSharpMinifier\CSharpMinify --locals --members --types --spaces --regions --comments --namespaces --to-string-methods --enum-to-int --line-length 100000 --skip-compile EasyAPI.lib.cs >> EasyAPI.min.cs
copy /b ^
  modules\BootstrapEasyAPI\BootstrapEasyAPI.cs +^
  EasyAPI.min.cs ^
  EasyAPI.cs
copy /b ^
  modules\BootstrapEasyAPI\BootstrapEasyAPI.cs +^
  EasyAPI.lib.cs ^
  EasyAPI.debug.cs
del EasyAPI.lib.cs
del EasyAPI.min.cs
pause
