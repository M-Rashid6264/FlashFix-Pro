# Known limitations

## Not yet physically implemented

- Raw sector access for physical USB/SD capacity verification.
- Metadata-aware NTFS, FAT, and exFAT deleted-file recovery.
- Signature carving and validation.
- Physical disk imaging.
- Partition-table analysis.
- TestDisk or PhotoRec integration.
- CHKDSK and DiskPart repair/format automation.
- Installer generation.

## Hardware constraints

Results depend on the card reader, controller, firmware, Windows drive-class, and administrator policies. A software write/read mismatch can indicate media/controller failure, but cannot prove that the storage chip is counterfeit. Capacity verification on real media requires a documented destructive test and user confirmation that is not included here.

## Testing boundary

Automated tests use simulated devices and disposable local test images. They never target the operating-system disk or physical removable media.
