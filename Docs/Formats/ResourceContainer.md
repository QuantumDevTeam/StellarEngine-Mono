# Resource container (`.sres`)

- [README.md](./../../README.md)

The binary container of compiled resources. It only says "a resource of this type consists of these chunks": what a type
and its chunks mean is defined by the module that owns the type.

All numbers are little-endian. The layout is mirrored by `Resources/ResourceContainer.h` of the native Kernel, so a
loaded file can be read in place.

## Layout

```
Header          16 bytes
Chunk table     24 bytes per chunk
Padding
Chunk data      each chunk starts at a multiple of 16 bytes
```

### Header

| Offset | Type | Field | Value |
|---|---|---|---|
| 0 | `u32` | `magic` | `SRES` |
| 4 | `u16` | `version` | 1 |
| 6 | `u16` | `flags` | 0, reserved |
| 8 | `u32` | `type` | FourCC of the resource type |
| 12 | `u32` | `chunkCount` | |

### Chunk table entry

| Offset | Type | Field | Value |
|---|---|---|---|
| 0 | `u32` | `tag` | FourCC of the chunk |
| 4 | `u32` | `reserved` | 0 |
| 8 | `u64` | `offset` | From the beginning of the file |
| 16 | `u64` | `size` | In bytes |

## FourCC

Four ASCII characters, padded with spaces, the first character in the lowest byte. `RAW ` is the type written by
`RawResourceCompiler`, with the content of the source file in a single `DATA` chunk.

Types and tags are matched as numbers. Pick ones that are unlikely to collide with another module.

## Versioning

A change of the layout above increases `version`, and readers reject versions they do not know. A change of what is
inside a chunk does not touch the container: it is the `Version` of the resource compiler.
