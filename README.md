# MetadataScanner

This project is designed to work with Markdown files that use metadata token/value pairs in their YAML blocks. The tool will scan lets a user do one of the following three actions:

1. Scan for metadata with an existing token/value pair and update the value.
2. Scan for metadata with an existing token but empty value, and fill in the value.
3. Scan for missing metadata tokens and insert the token with a value.

The project was initially designed to work with the Windows Drivers documentation, so it is predicated on a distinction between conceptual material and API reference material. There are two lists that must exist in the same directory where the executable is running, one for conceptual tokens and one for reference tokens. You can edit these lists as necessary to constrain the list of acceptable tokens. The tool can either blow in metadata/scan for metadata for all files in a directory, or on a file-by-file basis in the case of missing metadata tokens or values.
