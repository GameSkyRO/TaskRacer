using Google.Cloud.Firestore;

namespace TaskRacer.Models
{
    [FirestoreData]
    public class User
    {
        public string Id { get; set; }
        [FirestoreProperty]
        public string username { get; set; }
        [FirestoreProperty]
        public string password { get; set; }
        [FirestoreProperty]
        public List<string> projects { get; set; }
    }
}
