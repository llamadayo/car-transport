$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$dest = Join-Path $repo 'src/VD.Web/Prototype'
foreach($folder in @('css','js','js/vendor','docs')) { New-Item -ItemType Directory -Path (Join-Path $dest $folder) -Force | Out-Null }
Copy-Item (Join-Path $repo 'index.html') $dest -Force
Copy-Item (Join-Path $repo 'css/styles.css') (Join-Path $dest 'css') -Force
foreach($file in @('data.js','loadengine.js','moduleA.js','moduleB.js','moduleC.js','app.js')) { Copy-Item (Join-Path $repo "js/$file") (Join-Path $dest 'js') -Force }
Copy-Item (Join-Path $repo 'js/vendor/masonry.pkgd.min.js') (Join-Path $dest 'js/vendor') -Force
Copy-Item (Join-Path $repo 'docs/*.html') (Join-Path $dest 'docs') -Force
$appPath = Join-Path $dest 'js/app.js'
$app = Get-Content $appPath -Raw -Encoding UTF8
$app = $app.Replace("goto('dashboard');", "const initial = new URLSearchParams(location.search).get('page'); goto(/^[bc]_(apply|approve|review|driver)$/.test(initial || '') ? initial : 'dashboard');")
[IO.File]::WriteAllText($appPath, $app, (New-Object Text.UTF8Encoding($false)))
# Navigation handoff only; module B/C business code and data stay unchanged.
$bridge = @'
;(function () {
 var mvc = {a_apply:'Apply',a_review:'Tracking',a_dispatch:'Dispatch',a_masonry:'Cards',a_driver:'Driver'};
 var originalGoto = goto;
 goto = function (pageId) {
  if (mvc[pageId]) { location.href='../ModuleA/'+mvc[pageId]; return; }
  originalGoto(pageId);
 };
})();
'@
[IO.File]::AppendAllText($appPath, "`r`n"+$bridge, (New-Object Text.UTF8Encoding($false)))
Write-Host 'Prepared unchanged B/C prototype with MVC navigation handoff.'
