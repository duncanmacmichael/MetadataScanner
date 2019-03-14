# MetadataScanner

This project is designed to work with Markdown files that use metadata token/value pairs in their YAML blocks. The tool lets a user do one of the following three actions:

1. Scan for metadata with an existing token/value pair and update the value. This can only be done on all files at a time in the directory.
2. Scan for metadata with an existing token but empty value, and fill in the value. This can be done either on all files at a time in a directory, or per-file.
3. Scan for missing metadata tokens and insert the token with a value. This can be done either on all files at a time in a directory, or per-file.

The project was initially designed to work with the [Windows Drivers documentation](https://docs.microsoft.com/windows-hardware/drivers/index), so it is predicated on a distinction between conceptual material and API reference material. There are two lists that must exist in the same directory where the executable is running, one for conceptual tokens and one for reference tokens. You can edit these lists as necessary to constrain the list of acceptable tokens.
