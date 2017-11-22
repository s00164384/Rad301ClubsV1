namespace Rad301ClubsV1.Migrations.ClubModelMigrations
{
    using CsvHelper;
    using Models.ClubModel;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Text;

    internal sealed class Configuration : DbMigrationsConfiguration<Rad301ClubsV1.Models.ClubModel.ClubContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            MigrationsDirectory = @"Migrations\ClubModelMigrations";
        }

        protected override void Seed(Rad301ClubsV1.Models.ClubModel.ClubContext context)
        {
            SeedStudents(context);
            SeedClubs(context);

            SeedClubMembers(context);

        }

        private void SeedClubs(ClubContext context)
        {
            //List<Member> selectedMembers = SeedMembers(context);
            context.Clubs.AddOrUpdate(c => c.ClubName,
            new Club
            {
                ClubName = "The Tiddly Winks Club",
                CreationDate = DateTime.Now,
                adminID = -1, // Choosing a negative to define unassigned as all members will have a positive id later
                // It seem you cannot reliably assign the result of a method to a field while using 
                // Add Or Update. My suspicion is that it cannot evaluate whether 
                // or not it is an update. There could also be a EF version issue
                // The club events assignment will work though as it is 
                clubEvents = new List<ClubEvent>()
            {	// Create a new ClubEvent 
                        new ClubEvent { StartDateTime = DateTime.Now.Subtract( new TimeSpan(5,0,0,0,0)),
                           EndDateTime = DateTime.Now.Subtract( new TimeSpan(5,0,0,0,0)),
                           Location="Sligo", Venue="Arena",
                           // Update attendees with a method similar to the SeedClubMembers 
                           // See below
                        },
                        new ClubEvent { StartDateTime = DateTime.Now.Subtract( new TimeSpan(3,0,0,0,0)),
                           EndDateTime = DateTime.Now.Subtract( new TimeSpan(3,0,0,0,0)),
                           Location="Sligo", Venue="Main Canteen"
        },
            }
            });
            context.SaveChanges();

        }

        //private static List<Member> SeedMembers(ClubContext context)
        //{
        //    List<Member> selectedMembers = new List<Member>();
        //    var randomSetStudent = context.Students.Select(s => new { s.StudentID, r = Guid.NewGuid() });
        //    List<string> subset = randomSetStudent.OrderBy(s => s.r).Select(s => s.StudentID.ToString()).Take(10).ToList();
        //    foreach (string s in subset)
        //    {
        //        selectedMembers.Add(
        //           new Member { StudentID = s}
        //           );
        //    }
        //    return selectedMembers;
        //}
        private List<Student> GetStudents(ClubContext context)
        {
            // Create a random list of student ids
            var randomSetStudent = context.Students.Select(s => new { s.StudentID, r = Guid.NewGuid() });
            // sort them and take 10
            List<string> subset = randomSetStudent.OrderBy(s => s.r)
                .Select(s => s.StudentID.ToString()).Take(10).ToList();
            // return the selected students as a relaized list
            return context.Students.Where(s => subset.Contains(s.StudentID)).ToList();
        }

        private void SeedClubMembers(ClubContext context)
        {
            // Create a list to hold students
            List<Student> selectedStudents = new List<Student>();
            // It's important that you save any newly created clubs before retrieving them as a list
            foreach (var club in context.Clubs.ToList())
            {
                // Get a set of members if none set yet
                if (club.clubMembers.Count() < 1)
                {
                    // get a set of random candidates see method below
                    selectedStudents = GetStudents(context);
                    foreach (var m in selectedStudents)
                    {
                        // Add a new member with a reference to a club
                        // EF will pick up on the join fields later
                        context.members.AddOrUpdate(member => member.StudentID,
                            new Member { ClubId = club.ClubId, StudentID = m.StudentID });
                    }
                }
            }
            context.SaveChanges();

        }


        private void SeedStudents(ClubContext context)
        {
            //context.Clubs.AddOrUpdate(c => c.ClubName,
            //new Club { ClubName = "The Tiddly Winks Club", CreationDate = DateTime.Now });
            //context.Clubs.AddOrUpdate(c => c.ClubName,
            //new Club { ClubName = "The Chess Club", CreationDate = DateTime.Now });

            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Rad301ClubsV1.Migrations.ClubModelMigrations.TestStudents.csv";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    CsvReader csvReader = new CsvReader(reader);
                    csvReader.Configuration.HasHeaderRecord = false;
                    csvReader.Configuration.WillThrowOnMissingField = false;
                    var testStudents = csvReader.GetRecords<Student>().ToArray();
                    context.Students.AddOrUpdate(s => s.StudentID, testStudents);
                }
            }

        }
    }
}
