# Feature ideas

This document captures product ideas that could make `Perfume Tracker` better at three things:

- easier perfume collection maintenance
- better day-to-day choice suggestions
- more engaging gamification

## Easier collection maintenance

### Collection hygiene

- **Collection health dashboard**: highlight perfumes with missing metadata, no image, no rating, no tags, or no recent interaction.
- **Batch editing**: support multi-select updates for tags, seasons, occasions, family, storage location, and ownership status.
- **Duplicate detector**: flag likely duplicates across bottle size variants, editions, and near-identical names.
- **Shelf and location tracking**: let users assign bottles to shelves, drawers, travel trays, or seasonal storage.

### Replenishment and lifecycle

- **Low-stock assistant**: estimate remaining juice from bottle size and spray history, then surface likely re-buy candidates.
- **Rebuy and sample workflow**: track whether a perfume should be re-bought, sampled again, decanted, or retired.
- **Declutter suggestions**: identify neglected bottles, overlapping profiles, and perfumes that no longer match current preferences.
- **Seasonal rotation prompts**: remind users to revisit warm-weather or cold-weather bottles when the season changes.

### Better maintenance loops

- **Inbox for collection tasks**: a lightweight queue for things like “add image”, “finish review”, “confirm notes”, or “update stock”.
- **Smart import cleanup**: after adding a perfume, suggest normalized metadata, family corrections, and missing fields.
- **Review nudges**: prompt the user to rate or review a perfume after enough wears have been logged.

## Better choice suggestions

### Context-aware picking

- **Daily shortlist**: suggest 3-5 perfumes for today using weather, time of day, temperature, occasion, and recent wear history.
- **Decision mode**: compare two perfumes head-to-head for a user-entered scenario like office, dinner, rain, or travel.
- **Rotation-aware picker**: avoid repeating recent wears unless the user explicitly wants familiar favorites.
- **Energy and mood matching**: choose scents based on mood inputs such as relaxed, confident, cozy, playful, or formal.

### Smarter reasoning

- **Why this pick?**: attach clear reasoning to each suggestion, such as season fit, longevity, compliment potential, novelty, or wardrobe balance.
- **Underused gems**: bias some recommendations toward bottles the user owns but rarely reaches for.
- **Goal-based suggestions**: support outcome-driven prompts like office-safe, date-night, signature scent, focus boost, or comfort scent.
- **Occasion memory**: learn from what the user actually wore in similar situations and improve future recommendations.

### Discovery and exploration

- **Collection gaps**: suggest what type of perfume the wardrobe is missing, such as a fresh office scent or a winter evening option.
- **House and accord exploration**: recommend next wears based on houses, accords, or families the user has been enjoying lately.
- **A/B feedback loop**: let users choose between two suggested scents so the system learns preference boundaries faster.

## Gamification

### Progress and rewards

- **Curator quests**: award XP for maintaining the collection, such as completing metadata, uploading images, or reviewing neglected bottles.
- **Exploration badges**: reward wearing across different houses, families, accords, seasons, or occasions.
- **Personal records**: celebrate milestones like longest streak, most diverse month, or biggest rediscovery streak.
- **Taste evolution timeline**: show how preferences change over time and unlock milestones for new phases of the journey.

### Challenges

- **Rotation challenges**: examples include “7-day no-repeat challenge” or “wear every blue fragrance this month”.
- **Seasonal missions**: create limited-time goals tied to weather, holidays, or wardrobe transitions.
- **Rediscovery quests**: reward the user for revisiting old favorites or forgotten bottles.
- **Collection completeness goals**: encourage users to finish reviews, add notes, or classify every bottle in a category.

### Social-lite motivation

- **Monthly recap**: summarize usage, discoveries, streaks, and standout perfumes in a shareable format.
- **Theme weeks**: suggest a weekly challenge such as citrus week, iris week, or office-safe week.
- **Achievement chains**: unlock follow-up missions after completing related tasks, which makes progression feel more directed.

## Good candidates for first implementation

If you want the highest practical value first, these look like strong next steps:

1. **Collection health dashboard**
2. **Low-stock assistant**
3. **Daily shortlist with “why this pick?”**
4. **Rotation-aware picker**
5. **Curator quests tied to collection maintenance**
