# Defender Chronicles - Brand Handoff Acceptance Standard

Date: 2026-08-26
Related TODO: `NEXT-203`

`NEXT-203` must stay blocked until the accepted logo/splash task provides a concrete handoff. The project already contains brand assets and frontend tests, but this gate is about provenance and final product-owner acceptance of that specific brand direction.

## Validator

Pending template check:

```bash
python3 tools/validate_brand_handoff.py docs/evidence/visual-development/brand-handoff-template-2026-08-26.json --allow-pending
```

Final handoff check:

```bash
python3 tools/validate_brand_handoff.py docs/evidence/visual-development/brand-handoff-2026-08-26.json
```

## Required evidence

The final handoff JSON must include:

- accepted direction summary;
- source task title/status and handoff date;
- changed brand files;
- final Dynamic-Island-safe screenshot;
- boot, welcome and main-menu screenshots;
- brand capture directory;
- EditMode and PlayMode result references;
- product-owner, Dynamic Island and frontend regression signoffs.

The validator requires these files to exist in the workspace before it prints `[DC_BRAND_HANDOFF_PASS]`.

## Safety rule

Do not edit brand, frontend theme/catalog, `AppFlowController`, iOS settings or related tests from this visual closeout task until this validator passes on the final handoff file.
