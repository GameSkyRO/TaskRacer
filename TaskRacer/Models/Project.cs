using Google.Cloud.Firestore;

namespace TaskRacer.Models
{
    [FirestoreData]
    public class Project
    {
        public string ProjID { get; set; }
        [FirestoreProperty]
        public string proj_name { get; set; }
        [FirestoreProperty]
        public List<string> users { get; set; }

    }
}
