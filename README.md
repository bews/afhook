afhook2
======

Hacky tools for translating rUGP based games

Updated version:
1. Compiles with Visual Studio 2019 on Win11
2. Plugin: safe file saving feature added - now if the game was closed incorrectly the .pkg file won't become corrupted
3. Plugin: added a hacky message id feature for proper context usage with fallback (old .pkg files are backwards compatible) - works most of time (enabled only for HandleText5 for now, since I don't have other games to test this on)
4. Plugin: removed libpng16 and zlib dependancy, now uses stb_image_write instead
5. Editor: should work a bit faster now
6. Editor: added "Add missing lines to pkg" feature to force add some rare lines to the .pkg that were extracted using riox tool, but weren't found while playing the game (rare condition combo)
7. Editor: a compiled version to export/import UI (was too lazy to add this properly)
8. Compiled versions for plugin and editor
