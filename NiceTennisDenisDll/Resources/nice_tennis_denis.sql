SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

CREATE TABLE `configuration` (
  `id` int(10) UNSIGNED NOT NULL,
  `best_performances_count_for_ranking` int(10) UNSIGNED NOT NULL,
  `ranking_weeks_count` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `edition` (
  `id` int(10) UNSIGNED NOT NULL,
  `year` int(10) UNSIGNED NOT NULL,
  `code` varchar(255) COLLATE utf8_bin NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `surface_id` int(10) UNSIGNED DEFAULT NULL,
  `indoor` tinyint(1) NOT NULL DEFAULT '0',
  `draw_size` int(10) UNSIGNED DEFAULT NULL,
  `level_id` int(10) UNSIGNED NOT NULL,
  `date_begin` datetime NOT NULL,
  `date_end` datetime NOT NULL,
  `tournament_id` int(10) UNSIGNED NOT NULL,
  `slot_id` int(10) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `entry` (
  `id` int(10) UNSIGNED NOT NULL,
  `code` varchar(10) COLLATE utf8_bin NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `grid_point` (
  `level_id` int(10) UNSIGNED NOT NULL,
  `round_id` int(10) UNSIGNED NOT NULL,
  `points` int(10) UNSIGNED NOT NULL,
  `participation_points` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `level` (
  `id` int(10) UNSIGNED NOT NULL,
  `code` varchar(10) COLLATE utf8_bin NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `display_order` int(10) UNSIGNED NOT NULL,
  `mandatory` tinyint(1) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `match_general` (
  `id` int(10) UNSIGNED NOT NULL,
  `edition_id` int(10) UNSIGNED NOT NULL,
  `match_num` int(10) UNSIGNED NOT NULL,
  `best_of` int(10) UNSIGNED NOT NULL,
  `round_id` int(10) UNSIGNED NOT NULL,
  `minutes` int(10) UNSIGNED DEFAULT NULL,
  `winner_id` int(10) UNSIGNED NOT NULL,
  `winner_seed` int(10) UNSIGNED DEFAULT NULL,
  `winner_entry_id` int(10) UNSIGNED DEFAULT NULL,
  `winner_rank` int(10) UNSIGNED DEFAULT NULL,
  `winner_rank_points` int(10) UNSIGNED DEFAULT NULL,
  `loser_id` int(10) UNSIGNED NOT NULL,
  `loser_seed` int(10) UNSIGNED DEFAULT NULL,
  `loser_entry_id` int(10) UNSIGNED DEFAULT NULL,
  `loser_rank` int(10) UNSIGNED DEFAULT NULL,
  `loser_rank_points` int(10) UNSIGNED DEFAULT NULL,
  `walkover` tinyint(1) NOT NULL DEFAULT '0',
  `retirement` tinyint(1) NOT NULL DEFAULT '0',
  `disqualification` tinyint(1) NOT NULL DEFAULT '0',
  `unfinished` tinyint(1) NOT NULL DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `match_score` (
  `match_id` int(10) UNSIGNED NOT NULL,
  `w_set_1` int(10) UNSIGNED DEFAULT NULL,
  `l_set_1` int(10) UNSIGNED DEFAULT NULL,
  `tb_set_1` int(10) UNSIGNED DEFAULT NULL,
  `w_set_2` int(10) UNSIGNED DEFAULT NULL,
  `l_set_2` int(10) UNSIGNED DEFAULT NULL,
  `tb_set_2` int(10) UNSIGNED DEFAULT NULL,
  `w_set_3` int(10) UNSIGNED DEFAULT NULL,
  `l_set_3` int(10) UNSIGNED DEFAULT NULL,
  `tb_set_3` int(10) UNSIGNED DEFAULT NULL,
  `w_set_4` int(10) UNSIGNED DEFAULT NULL,
  `l_set_4` int(10) UNSIGNED DEFAULT NULL,
  `tb_set_4` int(10) UNSIGNED DEFAULT NULL,
  `w_set_5` int(10) UNSIGNED DEFAULT NULL,
  `l_set_5` int(10) UNSIGNED DEFAULT NULL,
  `tb_set_5` int(10) UNSIGNED DEFAULT NULL,
  `super_tb` varchar(255) COLLATE utf8_bin DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `match_stat` (
  `match_id` int(10) UNSIGNED NOT NULL,
  `w_ace` int(10) UNSIGNED DEFAULT NULL,
  `l_ace` int(10) UNSIGNED DEFAULT NULL,
  `w_df` int(10) UNSIGNED DEFAULT NULL,
  `l_df` int(10) UNSIGNED DEFAULT NULL,
  `w_sv_pt` int(10) UNSIGNED DEFAULT NULL,
  `l_sv_pt` int(10) UNSIGNED DEFAULT NULL,
  `w_1st_in` int(10) UNSIGNED DEFAULT NULL,
  `l_1st_in` int(10) UNSIGNED DEFAULT NULL,
  `w_1st_won` int(10) UNSIGNED DEFAULT NULL,
  `l_1st_won` int(10) UNSIGNED DEFAULT NULL,
  `w_2nd_won` int(10) UNSIGNED DEFAULT NULL,
  `l_2nd_won` int(10) UNSIGNED DEFAULT NULL,
  `w_sv_gms` int(10) UNSIGNED DEFAULT NULL,
  `l_sv_gms` int(10) UNSIGNED DEFAULT NULL,
  `w_bp_saved` int(10) UNSIGNED DEFAULT NULL,
  `l_bp_saved` int(10) UNSIGNED DEFAULT NULL,
  `w_bp_faced` int(10) UNSIGNED DEFAULT NULL,
  `l_bp_faced` int(10) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `player` (
  `id` int(10) UNSIGNED NOT NULL,
  `first_name` varchar(255) COLLATE utf8_bin NOT NULL,
  `last_name` varchar(255) COLLATE utf8_bin NOT NULL,
  `hand` char(1) COLLATE utf8_bin DEFAULT NULL,
  `birth_date` datetime DEFAULT NULL,
  `country` char(3) COLLATE utf8_bin NOT NULL,
  `height` int(10) UNSIGNED DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `qualification_point` (
  `level_id` int(10) UNSIGNED NOT NULL,
  `draw_size_min` int(10) UNSIGNED NOT NULL,
  `points` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `ranking` (
  `version_id` int(10) UNSIGNED NOT NULL,
  `player_id` int(10) UNSIGNED NOT NULL,
  `date` datetime NOT NULL,
  `points` int(10) UNSIGNED NOT NULL,
  `ranking` int(10) UNSIGNED NOT NULL,
  `editions` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `ranking_rule` (
  `id` int(10) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `ranking_version` (
  `id` int(10) UNSIGNED NOT NULL,
  `creation_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `ranking_version_rule` (
  `version_id` int(10) UNSIGNED NOT NULL,
  `rule_id` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `round` (
  `id` int(10) UNSIGNED NOT NULL,
  `code` varchar(10) COLLATE utf8_bin NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `players_count` int(10) UNSIGNED NOT NULL,
  `importance` int(10) UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `slot` (
  `id` int(10) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL,
  `display_order` int(10) UNSIGNED NOT NULL,
  `level_id` int(10) UNSIGNED NOT NULL,
  `mandatory` tinyint(1) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `source_matches` (
  `id` bigint(20) UNSIGNED NOT NULL,
  `file_name` varchar(255) COLLATE utf8_bin NOT NULL,
  `date_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `date_processed` datetime DEFAULT NULL,
  `tourney_id` varchar(255) COLLATE utf8_bin NOT NULL,
  `tourney_name` varchar(255) COLLATE utf8_bin NOT NULL,
  `surface` varchar(255) COLLATE utf8_bin NOT NULL,
  `draw_size` varchar(255) COLLATE utf8_bin NOT NULL,
  `tourney_level` varchar(255) COLLATE utf8_bin NOT NULL,
  `tourney_date` varchar(255) COLLATE utf8_bin NOT NULL,
  `match_num` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_id` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_seed` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_entry` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_name` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_hand` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_ht` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_ioc` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_age` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_id` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_seed` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_entry` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_name` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_hand` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_ht` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_ioc` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_age` varchar(255) COLLATE utf8_bin NOT NULL,
  `score` varchar(255) COLLATE utf8_bin NOT NULL,
  `best_of` varchar(255) COLLATE utf8_bin NOT NULL,
  `round` varchar(255) COLLATE utf8_bin NOT NULL,
  `minutes` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_ace` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_df` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_svpt` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_1stIn` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_1stWon` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_2ndWon` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_SvGms` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_bpSaved` varchar(255) COLLATE utf8_bin NOT NULL,
  `w_bpFaced` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_ace` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_df` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_svpt` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_1stIn` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_1stWon` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_2ndWon` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_SvGms` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_bpSaved` varchar(255) COLLATE utf8_bin NOT NULL,
  `l_bpFaced` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_rank` varchar(255) COLLATE utf8_bin NOT NULL,
  `winner_rank_points` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_rank` varchar(255) COLLATE utf8_bin NOT NULL,
  `loser_rank_points` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `source_players` (
  `player_id` varchar(255) COLLATE utf8_bin NOT NULL,
  `date_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `date_processed` datetime DEFAULT NULL,
  `name_first` varchar(255) COLLATE utf8_bin NOT NULL,
  `name_list` varchar(255) COLLATE utf8_bin NOT NULL,
  `hand` varchar(255) COLLATE utf8_bin NOT NULL,
  `birthdate` varchar(255) COLLATE utf8_bin NOT NULL,
  `country` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `surface` (
  `id` int(10) UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;

CREATE TABLE `tournament` (
  `id` int(10) UNSIGNED NOT NULL,
  `known_codes` varchar(255) COLLATE utf8_bin NOT NULL,
  `name` varchar(255) COLLATE utf8_bin NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_bin;


ALTER TABLE `configuration`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `edition`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `year` (`year`,`code`),
  ADD KEY `surface_id` (`surface_id`),
  ADD KEY `level_id` (`level_id`),
  ADD KEY `tournament_id` (`tournament_id`),
  ADD KEY `slot_id` (`slot_id`),
  ADD KEY `year_2` (`year`);

ALTER TABLE `entry`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `code` (`code`);

ALTER TABLE `grid_point`
  ADD PRIMARY KEY (`level_id`,`round_id`),
  ADD KEY `level_id` (`level_id`),
  ADD KEY `round_id` (`round_id`);

ALTER TABLE `level`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `code` (`code`);

ALTER TABLE `match_general`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `edition_id` (`edition_id`,`match_num`),
  ADD KEY `round_id` (`round_id`),
  ADD KEY `winner_id` (`winner_id`),
  ADD KEY `winner_entry_id` (`winner_entry_id`),
  ADD KEY `loser_id` (`loser_id`),
  ADD KEY `loser_entry_id` (`loser_entry_id`);

ALTER TABLE `match_score`
  ADD PRIMARY KEY (`match_id`);

ALTER TABLE `match_stat`
  ADD PRIMARY KEY (`match_id`);

ALTER TABLE `player`
  ADD PRIMARY KEY (`id`),
  ADD KEY `country` (`country`);

ALTER TABLE `qualification_point`
  ADD PRIMARY KEY (`level_id`,`draw_size_min`),
  ADD KEY `level_id` (`level_id`) USING BTREE;

ALTER TABLE `ranking`
  ADD PRIMARY KEY (`version_id`,`player_id`,`date`),
  ADD KEY `player_id` (`player_id`),
  ADD KEY `date` (`date`) USING BTREE,
  ADD KEY `version_id` (`version_id`),
  ADD KEY `version_id_2` (`version_id`);

ALTER TABLE `ranking_rule`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `ranking_version`
  ADD PRIMARY KEY (`id`);

ALTER TABLE `ranking_version_rule`
  ADD PRIMARY KEY (`version_id`,`rule_id`),
  ADD KEY `version_id` (`version_id`),
  ADD KEY `rule_id` (`rule_id`);

ALTER TABLE `round`
  ADD PRIMARY KEY (`id`),
  ADD KEY `code` (`code`);

ALTER TABLE `slot`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `display_order` (`display_order`,`level_id`),
  ADD KEY `level_id` (`level_id`);

ALTER TABLE `source_matches`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `tourney_id` (`tourney_id`,`match_num`),
  ADD KEY `file_name` (`file_name`);

ALTER TABLE `source_players`
  ADD PRIMARY KEY (`player_id`);

ALTER TABLE `surface`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `name` (`name`);

ALTER TABLE `tournament`
  ADD PRIMARY KEY (`id`);


ALTER TABLE `configuration`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `edition`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `entry`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `level`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `match_general`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `ranking_rule`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `ranking_version`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `round`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `slot`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `source_matches`
  MODIFY `id` bigint(20) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `surface`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;
ALTER TABLE `tournament`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT;

ALTER TABLE `edition`
  ADD CONSTRAINT `edition_ibfk_1` FOREIGN KEY (`slot_id`) REFERENCES `slot` (`id`),
  ADD CONSTRAINT `edition_ibfk_2` FOREIGN KEY (`tournament_id`) REFERENCES `tournament` (`id`),
  ADD CONSTRAINT `edition_ibfk_3` FOREIGN KEY (`surface_id`) REFERENCES `surface` (`id`),
  ADD CONSTRAINT `edition_ibfk_4` FOREIGN KEY (`level_id`) REFERENCES `level` (`id`);

ALTER TABLE `grid_point`
  ADD CONSTRAINT `grid_point_ibfk_1` FOREIGN KEY (`level_id`) REFERENCES `level` (`id`),
  ADD CONSTRAINT `grid_point_ibfk_2` FOREIGN KEY (`round_id`) REFERENCES `round` (`id`);

ALTER TABLE `match_general`
  ADD CONSTRAINT `match_general_ibfk_1` FOREIGN KEY (`edition_id`) REFERENCES `edition` (`id`),
  ADD CONSTRAINT `match_general_ibfk_2` FOREIGN KEY (`round_id`) REFERENCES `round` (`id`),
  ADD CONSTRAINT `match_general_ibfk_3` FOREIGN KEY (`winner_id`) REFERENCES `player` (`id`),
  ADD CONSTRAINT `match_general_ibfk_4` FOREIGN KEY (`loser_id`) REFERENCES `player` (`id`),
  ADD CONSTRAINT `match_general_ibfk_5` FOREIGN KEY (`winner_entry_id`) REFERENCES `entry` (`id`),
  ADD CONSTRAINT `match_general_ibfk_6` FOREIGN KEY (`loser_entry_id`) REFERENCES `entry` (`id`);

ALTER TABLE `match_score`
  ADD CONSTRAINT `match_score_ibfk_1` FOREIGN KEY (`match_id`) REFERENCES `match_general` (`id`);

ALTER TABLE `match_stat`
  ADD CONSTRAINT `match_stat_ibfk_1` FOREIGN KEY (`match_id`) REFERENCES `match_general` (`id`);

ALTER TABLE `qualification_point`
  ADD CONSTRAINT `qualification_point_ibfk_1` FOREIGN KEY (`level_id`) REFERENCES `level` (`id`);

ALTER TABLE `ranking`
  ADD CONSTRAINT `ranking_ibfk_1` FOREIGN KEY (`player_id`) REFERENCES `player` (`id`),
  ADD CONSTRAINT `ranking_ibfk_2` FOREIGN KEY (`version_id`) REFERENCES `ranking_version` (`id`);

ALTER TABLE `ranking_version_rule`
  ADD CONSTRAINT `ranking_version_rule_ibfk_1` FOREIGN KEY (`rule_id`) REFERENCES `ranking_rule` (`id`),
  ADD CONSTRAINT `ranking_version_rule_ibfk_2` FOREIGN KEY (`version_id`) REFERENCES `ranking_version` (`id`);

ALTER TABLE `slot`
  ADD CONSTRAINT `slot_ibfk_1` FOREIGN KEY (`level_id`) REFERENCES `level` (`id`);
