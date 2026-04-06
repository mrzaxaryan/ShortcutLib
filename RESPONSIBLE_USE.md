# Responsible Use Policy

This repository contains a library for creating, parsing, and editing Windows Shell Link (.lnk) shortcut files, including metadata sanitization, forensic data extraction, and WinX menu hash computation.

## Intended Use

This project is intended for:
- Software development requiring programmatic shortcut management
- Digital forensics and incident response (DFIR)
- Security research into shortcut-based attack vectors
- Malware analysis involving .lnk file artifacts
- System administration and deployment automation
- Educational purposes in understanding the MS-SHLLINK specification

## Authorization Requirement

When using this library for security analysis or forensics:
- Only analyze files you own or have explicit authorization to examine
- Follow chain-of-custody procedures when handling forensic evidence
- Respect your organization's policies on artifact analysis

## Prohibited Use

You may NOT use this software:
- To craft malicious shortcut files for phishing or social engineering attacks
- To tamper with shortcuts on systems without authorization
- To sanitize forensic evidence to obstruct investigations
- To manipulate WinX menu entries for privilege escalation on unauthorized systems
- For any illegal, unethical, or malicious activity

## Detection & Risk Notice

Shortcut files created or modified by this library may:
- Trigger security product alerts if they contain unusual metadata patterns
- Be flagged by email gateways or endpoint protection when distributed
- Contain forensic artifacts (TrackerData, MAC addresses) if not explicitly sanitized

## No Warranty / Liability

This software is provided "AS IS", without warranty of any kind, express or implied.

The authors are not responsible for:
- Damage to systems or data loss
- Legal consequences resulting from misuse
- Any harm caused by unauthorized or unethical use
