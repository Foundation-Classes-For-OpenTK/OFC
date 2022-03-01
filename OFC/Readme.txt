Documentation flow:

Ensure all xml comments are made

Build in release mode, .md documents are made to bin/debug/mddoc

In the mddoc folder:

run eddtest xmltomd . *.md OpenTK.Graphics.OpenGL c:\code\ofc\ofc\docexternlinks.txt opengl

This will output any OpenTK.Graphics.OpenGL types which are not in docexternlinks.txt, if so, add them.

OpenTK.Graphics seems to be documented by microsoft and thus they can be left.

Add any to the docexternlinks.txt file.

In the OFC Wiki:

Use the copywiki.bat file. This will copy the .md files from \bin\mddoc to the wiki, then run the EDDTEST mddoc post processor to adjust the formatting.

Status:

GL OpenTK Foundation Classes

Samplers
Logical Ops 387
Multisample Textures 417		- added support in texture2d/2darray to make them
Clip Control ?
Compression Support 516

Sparse textures are an ARB thing not in core

