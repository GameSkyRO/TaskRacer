using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using TaskRacer.Models;
using TaskRacer.DataAccess;
using Google.Apis.Util;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller;

namespace TaskRacer.Controllers
{
    public class NewController : Controller
    {

        DataAccess.DataAccess dataAccess = new DataAccess.DataAccess();
        private readonly IHttpContextAccessor _httpContextAccessor;
        ISession session;

        public NewController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            session = _httpContextAccessor.HttpContext.Session;

        }


        public async Task<string> CreateProjectObj(Project project)
        {
            DocumentReference addProjectToUser = dataAccess.firestoreDb.Collection("Users").Document(project.users.First<string>());
            DocumentReference docRef = await dataAccess.firestoreDb.Collection("Projects").AddAsync(project);


            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "projects", FieldValue.ArrayUnion(docRef.Id) }
            };

            await addProjectToUser.UpdateAsync(updates);

            return docRef.Id;
        }

        public async Task CreateProgressObj(Progress prog,string proj_id)
        {
            DocumentReference docRef = await dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").AddAsync(prog);

            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "users", FieldValue.ArrayUnion(session.GetString("UserID")) }
            };

            await docRef.UpdateAsync(updates);
        }

        public async Task AddTask(Tsk task, string pid, string proj_id)
		{
			DocumentReference docRef = await dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid).Collection("Tasks").AddAsync(task);
		}

        //---------------------------------------------------------------------------------------------------------------------------//


        [HttpPost]
        public async Task<string> CreateProject(Project project)
        {
            string id="";
            try
            {
                id = await CreateProjectObj(project);
                ModelState.AddModelError(string.Empty, "Added successfuly!");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return id;
        }
        public async Task<IActionResult> AddUserToProject(string proj_id,string user_ID)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id);
            DocumentReference addProjectToUser = dataAccess.firestoreDb.Collection("Users").Document(user_ID);
            DocumentSnapshot addProjectToUserSnap = await addProjectToUser.GetSnapshotAsync();

            if (addProjectToUserSnap.Exists)
            {
                Dictionary<string, object> upd = new Dictionary<string, object>
                {
                    { "projects", FieldValue.ArrayUnion(docRef.Id) }
                };
                await addProjectToUser.UpdateAsync(upd);

                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "users", FieldValue.ArrayUnion(user_ID) }
                };
                await docRef.UpdateAsync(updates);
                return Ok();
            }
            else return NotFound("This user doesn't exist!");
            
        }

        public async Task DeleteUserFromProject(string user, string proj_id)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id);
            Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "users", FieldValue.ArrayRemove(user) }
                };
            await docRef.UpdateAsync(updates);

            docRef = dataAccess.firestoreDb.Collection("Users").Document(user);
            Dictionary<string, object> update = new Dictionary<string, object>
                {
                    { "projects", FieldValue.ArrayRemove(proj_id) }
                };
            await docRef.UpdateAsync(update);
        }

        public async Task<IActionResult> ShowProjects()
        {
            Debug.WriteLine("Here"+session.GetString("UserID"));
            ViewBag.currentUser = session.GetString("UserID");
            List<Project> lstProject = new List<Project>();
            Query allTasksQuery = dataAccess.firestoreDb.Collection("Projects");
            QuerySnapshot allTasksQuerySnapshot = await allTasksQuery.GetSnapshotAsync();
            foreach (DocumentSnapshot documentSnapshot in allTasksQuerySnapshot.Documents)
            {

                Project project = documentSnapshot.ConvertTo<Project>();
                project.ProjID = documentSnapshot.Id;
                if (project.users != null) {
                    if(project.users.Contains(session.GetString("UserID"))) lstProject.Add(project); }
            }
            return View(lstProject);
        }

        public async Task UpdateProject(string name, string proj_id)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id);
            Dictionary<string, object> updates = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(name)) updates.Add("proj_name", name);
            await docRef.UpdateAsync(updates);
        }

        public async Task DeleteProject(string proj_id)
        {
            Query allProgressQuery = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress");
            QuerySnapshot allProgressQuerySnapshot = await allProgressQuery.GetSnapshotAsync();
            foreach (DocumentSnapshot documentSnapshot in allProgressQuerySnapshot.Documents)
            {
                Query allTasksQuery = documentSnapshot.Reference.Collection("Tasks");
                QuerySnapshot allTasksQuerySnapshot = await allTasksQuery.GetSnapshotAsync();
                foreach (DocumentSnapshot snapshot in allTasksQuerySnapshot.Documents)
                {
                    await snapshot.Reference.DeleteAsync();
                }
                await documentSnapshot.Reference.DeleteAsync();
                
            }
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id);
            await docRef.DeleteAsync();

        }


        [HttpPost]
        public async Task CreateProgress(Progress prog, string proj_id)
        {
            try
            {
                await CreateProgressObj(prog, proj_id);
                ModelState.AddModelError(string.Empty, "Added successfuly!");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        public async Task<IActionResult> AddUserToProgress(string proj_id, string user_ID, string pid)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid);
            DocumentReference addProjectToUser = dataAccess.firestoreDb.Collection("Users").Document(user_ID);
            DocumentSnapshot addProjectToUserSnap = await addProjectToUser.GetSnapshotAsync();

            if (addProjectToUserSnap.Exists)
            {
                
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "users", FieldValue.ArrayUnion(user_ID) }
                };
                await docRef.UpdateAsync(updates);
                return Ok();
            }
            else return NotFound("This user doesn't exist!");

        }

        public async Task DeleteUserFromProgress(string user, string proj_id, string pid)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid);
            Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "users", FieldValue.ArrayRemove(user) }
                };
            await docRef.UpdateAsync(updates);

        }

        public async Task<IActionResult> ShowProgress(string proj_id)
        {
            bool access = false;
            ViewBag.access = true;
            ViewBag.proj_Id= proj_id;
            ViewBag.userID = session.GetString("UserID");
            ViewBag.currentUser = session.GetString("UserID");

            List<Progress> lstProg = new List<Progress>();
            Query allProgressQuery = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress");
            QuerySnapshot allProgressQuerySnapshot = await allProgressQuery.GetSnapshotAsync();

            //this part is responsible for fetching the users that have acces to the project
            DocumentReference allUsers = dataAccess.firestoreDb.Collection("Projects").Document(proj_id);
            DocumentSnapshot allUsersSnap = await allUsers.GetSnapshotAsync();
            if (allUsersSnap.Exists)
            {
                List<User> Users = new List<User>();
                Project proj = allUsersSnap.ConvertTo<Project>();
                ViewBag.owner = proj.users.First();
                foreach (string user_id in proj.users)
                {
                    DocumentReference user = dataAccess.firestoreDb.Collection("Users").Document(user_id);
                    DocumentSnapshot userSnap = await user.GetSnapshotAsync();
                    if (user_id == session.GetString("UserID")) access = true;

                    Users.Add(new User { username = userSnap.ConvertTo<User>().username, Id = user_id });
                }
                if (!access)
                {
                    ViewBag.access = false;
                    return View();
                }

                ViewBag.Array = Users;
            }
            else return NotFound("This project does not exist!");
            


            
            foreach (DocumentSnapshot documentSnapshot in allProgressQuerySnapshot.Documents)
            {
                Progress progress = documentSnapshot.ConvertTo<Progress>();
                progress.pid = documentSnapshot.Id;
                if(progress.users.Contains(session.GetString("UserID"))) lstProg.Add(progress);
            }

            return View(lstProg);
        }

        public async Task UpdateProgress(string name, string pid, string proj_id)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid);
            Dictionary<string, object> updates = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(name)) updates.Add("name", name);
            await docRef.UpdateAsync(updates);
        }


        public async Task DeleteProgress(string pid, string proj_id)
        {
            Query allTasksQuery = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid).Collection("Tasks");
            QuerySnapshot allTasksQuerySnapshot = await allTasksQuery.GetSnapshotAsync();
            foreach (DocumentSnapshot documentSnapshot in allTasksQuerySnapshot.Documents)
            {
                documentSnapshot.Reference.DeleteAsync();
            }
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid);
            await docRef.DeleteAsync();

        }

        public async Task CreateTask(Tsk task, string pid, string proj_id)
        {
            await AddTask(task, pid, proj_id);
        }


        public async Task<IActionResult> ShowTasks(string pid, string proj_id)
        {
            bool access = false;
            ViewBag.access = true;
            ViewBag.Id = pid;
            ViewBag.proj_Id = proj_id;
            ViewBag.currentUser = session.GetString("UserID");
            List<Tsk> lstTask = new List<Tsk>();
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            //this part is responsible for fetching the users that have access to the progress---------------
            DocumentReference allUsers = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid);
            DocumentSnapshot allUsersSnap = await allUsers.GetSnapshotAsync();
            if (allUsersSnap.Exists) {
                List<User> Users = new List<User>();
                Progress prog = allUsersSnap.ConvertTo<Progress>();
                ViewBag.owner = prog.users.First();
                foreach (string user_id in prog.users)
                {
                    DocumentReference user = dataAccess.firestoreDb.Collection("Users").Document(user_id);
                    DocumentSnapshot userSnap = await user.GetSnapshotAsync();
                    if (user_id == session.GetString("UserID")) access = true;

                    Users.Add(new User { username = userSnap.ConvertTo<User>().username, Id = user_id });
                }
                if (!access)
                {
                    ViewBag.access = false;
                    return View();
                }
                ViewBag.Array = Users;
            }
            else return NotFound("This progress doesn't exist!");
            
            
            //-------------------------------------------------------------------------------------------
            if (snapshot.Exists)
            {
                Query allTasksQuery = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid).Collection("Tasks");
                QuerySnapshot allTasksQuerySnapshot = await allTasksQuery.GetSnapshotAsync();
                foreach (DocumentSnapshot documentSnapshot in allTasksQuerySnapshot.Documents)
                {
                    Tsk tsk = documentSnapshot.ConvertTo<Tsk>();
                    tsk.id = documentSnapshot.Id;
                    lstTask.Add(tsk);
                }

                return View(lstTask);
            }

            else return View();
        }

        [HttpPost]
        public async Task UpdateTask(string task_name, string description, string reward, string task_id, string pid, string proj_id)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid).Collection("Tasks").Document(task_id);
            Dictionary<string, object> updates = new Dictionary<string, object>();
            if(!string.IsNullOrEmpty(task_name)) updates.Add("task_name", task_name);
            if (!string.IsNullOrEmpty(description)) updates.Add("description", description);
            if (!string.IsNullOrEmpty(reward)) updates.Add("reward", reward);
            await docRef.UpdateAsync(updates);
        }

        public async Task DeleteTask(string task, string pid, string proj_id)
        {
            DocumentReference docRef = dataAccess.firestoreDb.Collection("Projects").Document(proj_id).Collection("Progress").Document(pid).Collection("Tasks").Document(task);
            await docRef.DeleteAsync();
        }

        [HttpGet]
        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(string username, string password) 
        {

            Query allUsers = dataAccess.firestoreDb.Collection("Users");
            QuerySnapshot allUsersSnap = await allUsers.GetSnapshotAsync();
            foreach (DocumentSnapshot doc in allUsersSnap.Documents)
            {
                User user = doc.ConvertTo<User>();
                if (username == user.username && password == user.password)
                {
                    
                    session.SetString("UserID", doc.Id);
                    return Ok();
                }
                else if (username == user.username)
                {
                    return BadRequest("Incorrect password!");
                }
            }
            return NotFound("This user doesn't exist!");
        }

        [HttpGet]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(string username, string password)
        {
            bool found = false;
            Query allUsers = dataAccess.firestoreDb.Collection("Users");
            QuerySnapshot allUsersSnap = await allUsers.GetSnapshotAsync();
            foreach (DocumentSnapshot doc in allUsersSnap.Documents)
            {
                User user = doc.ConvertTo<User>();
                if (username == user.username)
                {
                    found = true;
                    
                }
            }
            if(found) return BadRequest("User already exists!");
            else {
                DocumentReference docRef = await dataAccess.firestoreDb.Collection("Users").AddAsync(new User { username = username, password = password });
                return Ok();
            }
                
            
        }

        public void SignOut()
        {
            session.SetString("UserID", "");
        }

    }
}
