using Google.Cloud.Firestore;
namespace TaskRacer.Models
{
    [FirestoreData]
    public class Tsk
    {
        public string id { get; set; }
        [FirestoreProperty]
        public int order_num { get; set; }
        [FirestoreProperty]
        public string task_name { get; set; }
        [FirestoreProperty]
        public string description { get; set; }
        [FirestoreProperty]
        public string reward { get; set; }
    }
}
