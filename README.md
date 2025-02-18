# Relocation Section Editor

An editor for the relocation table in Portable Executable (Windows PE) files.

### Notes

- This is a slightly modified version of the code published by Christophe Mohimont and credited to gta126 (whether these are the same person is not explicitly clear).
- Assumes the base address of the program being edited (i.e., does not necessarily start addresses at 0x0).
- Only works on 32-bit programs; you can try [Viacheslav Vasilyev's version](https://github.com/mohic/Relocation-Section-Editor/pull/1) for experimental 64-bit support.

### License

Because the originally published code does not include a license, the project can't be legally relicensed.

However, for any changes I've made from the original, please consider them to be licensed under MPL-2.0 (this may be useful if you ever receive express permission from the original author(s) to add a license to the originally published code).