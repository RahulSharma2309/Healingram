# Create a retreat

**Who would want this:** a partner or admin adding a new property.

## V1 truth

There is **no** “Create retreat” screen and **no** write API on the catalog. `/vendor#retreats` and `/admin#retreats` are navigation leftovers, not an editor.

Today’s stays exist because they were **seeded and published** in the catalog module.

Adding Goa or Himachal later is a **data + publish** change (and later, a real CMS). It is not a missing guest feature.

## What “done” looks like for V1

Public `/retreats` matches published seed. Unpublished rows stay hidden. The UI never says “retreat created” unless the catalog GET would return it.
