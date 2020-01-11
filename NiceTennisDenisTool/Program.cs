using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace NiceTennisDenisTool
{
    class Program
    {
        const string _connectionString = "Server=localhost;Database=first_for_mugu;Uid=root;Pwd=;";

        static void Main(string[] args)
        {
            List<Pl> pls = new List<Pl>();
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT p.id, p.first_name, p.last_name, e.name, e.year, e.id " +
                        "FROM match_general as mg join edition as e on mg.edition_id = e.id join player as p on mg.winner_id = p.id " +
                        "WHERE mg.round_id = 1 and e.level_id = 2";
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            var curr = new Pl
                            {
                                Year = rd.GetUInt32(4),
                                Tournament = rd.GetString(3),
                                Id = rd.GetUInt32(0),
                                Name = string.Concat(rd.GetString(2), ", ", rd.GetString(1))
                            };
                            using (var conn2 = new MySqlConnection(_connectionString))
                            {
                                conn2.Open();
                                using (var cmd2 = conn2.CreateCommand())
                                {
                                    cmd2.CommandText = "SELECT sum(ifnull(l_set_1, 0) + ifnull(l_set_2, 0) + ifnull(l_set_3, 0)) " +
                                        "FROM match_score as ms join match_general as mg on ms.match_id = mg.id " +
                                        $"WHERE mg.edition_id = {rd.GetUInt32(5)} and mg.winner_id = {curr.Id}";
                                    curr.CountGamesLost = Convert.ToInt32(cmd2.ExecuteScalar());
                                    cmd2.CommandText = "SELECT sum(if(ifnull(l_set_1, 0) > ifnull(w_set_1, 0), 1, 0) + if (ifnull(l_set_2, 0) > ifnull(w_set_2, 0), 1, 0)) " +
                                        "FROM match_score as ms join match_general as mg on ms.match_id = mg.id " +
                                        $"WHERE mg.edition_id = {rd.GetUInt32(5)} and mg.winner_id = {curr.Id}";
                                    curr.CountSetLost = Convert.ToInt32(cmd2.ExecuteScalar());
                                    cmd2.CommandText = "SELECT count(*) " +
                                        "FROM match_score as ms join match_general as mg on ms.match_id = mg.id " +
                                        $"WHERE mg.edition_id = {rd.GetUInt32(5)} and mg.winner_id = {curr.Id}";
                                    curr.CountMatchesPlayed = Convert.ToInt32(cmd2.ExecuteScalar());
                                    cmd2.CommandText = "SELECT count(*) " +
                                        "FROM match_score as ms join match_general as mg on ms.match_id = mg.id " +
                                        $"WHERE mg.edition_id = {rd.GetUInt32(5)} and mg.winner_id = {curr.Id} " +
                                        "and (walkover = 1 or retirement = 1 or disqualification = 1 or unfinished = 1)";
                                    curr.CountUnfinished = Convert.ToInt32(cmd2.ExecuteScalar());
                                }
                            }
                            pls.Add(curr);
                        }
                    }
                }
            }

            using (var writer = new System.IO.StreamWriter(@"S:\tor\toto.csv"))
            {
                writer.WriteLine("Id\tName\tYear\tTournament\tMatches\tSetsLost\tGamesLost\tCountUnfinished");
                foreach (var pl in pls)
                {
                    writer.WriteLine(pl);
                }
            }
        }
    }

    public class Pl
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Tournament { get; set; }
        public uint Year { get; set; }
        public int CountGamesLost { get; set; }
        public int CountSetLost { get; set; }
        public int CountMatchesPlayed { get; set; }
        public int CountUnfinished { get; set; }

        public override string ToString()
        {
            return string.Concat(Id, "\t", Name, "\t", Year,  "\t", Tournament, "\t", CountMatchesPlayed, "\t", CountSetLost, "\t", CountGamesLost, "\t", CountUnfinished);
        }
    }
}
