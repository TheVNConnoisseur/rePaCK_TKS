# rePaCK_TKS
Tool for unpacking and packing of .PCK files offered in ANIM and CROWD visual novel games.

### How are PCK files structured?
While the code also documents how a .PCK file is structured, here it is also the same information on a more accessible manner.

The file is divided into 4 parts:
  * **Header**: Number of files (4 bytes)
  * **File table**: we have `Number of files` entries, and each entry has: Dummy bytes (4 bytes) + Offset of the file raw data (4 bytes) + Size of the raw data in bytes (4 bytes)
    * Dummy bytes: 4 null bytes, they serve no purpose
    * Offset of the file raw data: the offset where the raw data section for the entry is
    * Size of the raw data: the amount of bytes the entry occupies in the raw data section
  * **Name files**: we have `Number of files` entries, and each entry has: File name with extension (Unknown bytes) + Null terminator (1 byte)
  * **Raw data**: the actual binary data each file contains.