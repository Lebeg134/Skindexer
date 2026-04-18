-- ============================================================
-- CS2 Rarity Order Seed
-- Run once after enrichment to assign display order to rarities
-- Re-runnable / idempotent
-- ============================================================

-- Agent
UPDATE rarities SET "order" = 0 WHERE slug = 'distinguished'  AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'agent');
UPDATE rarities SET "order" = 1 WHERE slug = 'superior'       AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'agent');
UPDATE rarities SET "order" = 2 WHERE slug = 'exceptional'    AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'agent');
UPDATE rarities SET "order" = 3 WHERE slug = 'master'         AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'agent');

-- Collectible
UPDATE rarities SET "order" = 0 WHERE slug = 'base_grade'      AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'collectible');
UPDATE rarities SET "order" = 1 WHERE slug = 'high_grade'      AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'collectible');
UPDATE rarities SET "order" = 2 WHERE slug = 'remarkable'      AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'collectible');
UPDATE rarities SET "order" = 3 WHERE slug = 'exotic'          AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'collectible');
UPDATE rarities SET "order" = 4 WHERE slug = 'extraordinary'   AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'collectible');

-- Container (sidegrades — order left null intentionally)

-- Graffiti
UPDATE rarities SET "order" = 0 WHERE slug = 'base_grade'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'graffiti');
UPDATE rarities SET "order" = 1 WHERE slug = 'high_grade'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'graffiti');
UPDATE rarities SET "order" = 2 WHERE slug = 'remarkable'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'graffiti');
UPDATE rarities SET "order" = 3 WHERE slug = 'exotic'         AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'graffiti');
UPDATE rarities SET "order" = 4 WHERE slug = 'extraordinary'  AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'graffiti');

-- Keychain
UPDATE rarities SET "order" = 0 WHERE slug = 'base_grade'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'keychain');
UPDATE rarities SET "order" = 1 WHERE slug = 'high_grade'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'keychain');
UPDATE rarities SET "order" = 2 WHERE slug = 'remarkable'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'keychain');
UPDATE rarities SET "order" = 3 WHERE slug = 'exotic'         AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'keychain');
UPDATE rarities SET "order" = 4 WHERE slug = 'extraordinary'  AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'keychain');

-- Music Kit (single rarity — order left null intentionally)

-- Patch
UPDATE rarities SET "order" = 0 WHERE slug = 'remarkable'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'patch');
UPDATE rarities SET "order" = 1 WHERE slug = 'high_grade'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'patch');
UPDATE rarities SET "order" = 2 WHERE slug = 'exotic'         AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'patch');

-- Weapon Skin
UPDATE rarities SET "order" = 0 WHERE slug = 'consumer_grade'    AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
UPDATE rarities SET "order" = 1 WHERE slug = 'industrial_grade'  AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
UPDATE rarities SET "order" = 2 WHERE slug = 'mil-spec_grade'    AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
UPDATE rarities SET "order" = 3 WHERE slug = 'restricted'        AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
UPDATE rarities SET "order" = 4 WHERE slug = 'classified'        AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
UPDATE rarities SET "order" = 5 WHERE slug = 'covert'            AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
UPDATE rarities SET "order" = 6 WHERE slug = 'extraordinary'     AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
UPDATE rarities SET "order" = 7 WHERE slug = 'contraband'        AND rarity_group_id = (SELECT id FROM rarity_groups WHERE game_id = 'cs2' AND type = 'weapon_skin');
