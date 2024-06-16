using Google.Cloud.Firestore;

namespace TaskRacer.Models
{
    [FirestoreData]
    public class Progress
    {
		public string pid { get; set; }
		[FirestoreProperty]
        public string name { get; set; }
        [FirestoreProperty]
        public List<string> users { get; set; }
        public List<Tsk> tasks { get; set; }


    }
}
