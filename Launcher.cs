using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ScrcpyLauncher
{
    // Struttura per definire un flag di scrcpy
    class ScrcpyFlag
    {
        public string Flag { get; set; }
        public string Description { get; set; }
        public bool RequiresInput { get; set; }
        public string UserInput { get; set; }
        public bool IsSelected { get; set; }

        public ScrcpyFlag(string flag, string desc, bool reqInput = false)
        {
            Flag = flag;
            Description = desc;
            RequiresInput = reqInput;
            UserInput = "";
            IsSelected = false;
        }

        public ScrcpyFlag Clone()
        {
            return new ScrcpyFlag(Flag, Description, RequiresInput)
            {
                UserInput = this.UserInput,
                IsSelected = this.IsSelected
            };
        }
    }

    class Program
    {
        // FILE UNICO PER DATI E CONFIGURAZIONE
        static string DATA_FILE = "scrcpy_data.txt";
        static string CONFIG_PREFIX = "__CONFIG__|";
        
        static string ScrcpyFolder = ""; 
        static List<ScrcpyFlag> MasterFlagList = new List<ScrcpyFlag>();

        static void Main(string[] args)
        {
            Console.Title = "Scrcpy Launcher Custom";
            Console.CursorVisible = false;
            
            LoadConfig();
            InitializeFlags();

            while (true)
            {
                ShowMainMenu();
            }
        }

        // --- GESTIONE DATI (FILE UNICO) ---

        static void LoadConfig()
        {
            if (!File.Exists(DATA_FILE)) return;

            string[] lines = File.ReadAllLines(DATA_FILE);
            foreach (string line in lines)
            {
                if (line.StartsWith(CONFIG_PREFIX))
                {
                    ScrcpyFolder = line.Substring(CONFIG_PREFIX.Length);
                    break;
                }
            }
        }

        static List<string> LoadProfiles()
        {
            if (!File.Exists(DATA_FILE)) return new List<string>();

            return File.ReadAllLines(DATA_FILE)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith(CONFIG_PREFIX))
                .ToList();
        }

        static void SaveAllData(List<string> profiles)
        {
            List<string> linesToWrite = new List<string>();
            linesToWrite.Add(CONFIG_PREFIX + ScrcpyFolder);
            linesToWrite.AddRange(profiles);

            try
            {
                File.WriteAllLines(DATA_FILE, linesToWrite.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore nel salvataggio dati: " + ex.Message);
                Console.ReadKey();
            }
        }

        static void SetScrcpyFolder()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== IMPOSTA CARTELLA SCRCPY ===");
            Console.ResetColor();
            Console.WriteLine("Attuale: " + (string.IsNullOrEmpty(ScrcpyFolder) ? "NON IMPOSTATO" : ScrcpyFolder));
            Console.WriteLine("\nIncolla il percorso della cartella dove si trova scrcpy.exe:");
            
            Console.CursorVisible = true;
            Console.Write("> ");
            string input = Console.ReadLine();
            Console.CursorVisible = false;

            if (string.IsNullOrWhiteSpace(input)) return;

            input = input.Replace("\"", "");

            if (input.ToLower().EndsWith("scrcpy.exe"))
            {
                input = Path.GetDirectoryName(input);
            }

            string fullPath = Path.Combine(input, "scrcpy.exe");
            if (File.Exists(fullPath))
            {
                ScrcpyFolder = input;
                SaveAllData(LoadProfiles());
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nPercorso salvato con successo!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nERRORE: scrcpy.exe non trovato in questa cartella.");
            }
            
            Console.ResetColor();
            Console.WriteLine("\nPremi un tasto per tornare indietro...");
            Console.ReadKey();
        }

        // --- MENU PRINCIPALE ---
        static void ShowMainMenu()
        {
            List<string> profiles = LoadProfiles();
            int selectedIndex = 0;
            
            while (true)
            {
                int totalItems = profiles.Count + 1;
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== SCRCPY LAUNCHER ===");
                Console.ResetColor();
                
                if (string.IsNullOrEmpty(ScrcpyFolder))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ATTENZIONE] Percorso scrcpy non impostato! Vai su Opzioni.");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("Folder: " + ScrcpyFolder);
                }
                Console.WriteLine();

                for (int i = 0; i < profiles.Count; i++)
                {
                    string profileName = profiles[i].Split('|')[0];
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine("> " + profileName);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("  " + profileName);
                    }
                }

                if (selectedIndex == profiles.Count)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine("> [ OPZIONI - Gestione Profili e Percorso ]");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("  [ OPZIONI - Gestione Profili e Percorso ]");
                }

                Console.WriteLine("\n------------------------------------------------");
                Console.WriteLine("FRECCE: Naviga | INVIO/X: Lancia");
                Console.WriteLine("SHIFT + INVIO/X: Scegli App dal dispositivo e lancia");

                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.UpArrow)
                {
                    selectedIndex--;
                    if (selectedIndex < 0) selectedIndex = totalItems - 1;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectedIndex++;
                    if (selectedIndex >= totalItems) selectedIndex = 0;
                }
                else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.X)
                {
                    if (selectedIndex == profiles.Count)
                    {
                        ShowOptionsMenu();
                        profiles = LoadProfiles(); 
                        selectedIndex = 0;
                    }
                    else
                    {
                        // Controlla se SHIFT è premuto
                        bool isShiftPressed = (key.Modifiers & ConsoleModifiers.Shift) != 0;

                        if (isShiftPressed)
                        {
                            // Modalità selezione app
                            string selectedApp = SelectAppFromDevice();
                            if (!string.IsNullOrEmpty(selectedApp))
                            {
                                string originalProfile = profiles[selectedIndex];
                                string modifiedProfile = InjectStartApp(originalProfile, selectedApp);
                                LaunchScrcpy(modifiedProfile);
                            }
                        }
                        else
                        {
                            // Modalità normale
                            LaunchScrcpy(profiles[selectedIndex]);
                        }
                    }
                }
            }
        }

        // --- MENU OPZIONI ---
        static void ShowOptionsMenu()
        {
            string[] options = { 
                "Imposta cartella Scrcpy", 
                "Crea Nuovo Profilo", 
                "Modifica Profilo Esistente", 
                "Elimina Profilo", 
                "Indietro" 
            };
            int selectedIndex = 0;

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== OPZIONI ===");
                Console.ResetColor();
                
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine("> " + options[i]);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("  " + options[i]);
                    }
                }

                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.UpArrow)
                {
                    selectedIndex--;
                    if (selectedIndex < 0) selectedIndex = options.Length - 1;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectedIndex++;
                    if (selectedIndex >= options.Length) selectedIndex = 0;
                }
                else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.X)
                {
                    switch (selectedIndex)
                    {
                        case 0: SetScrcpyFolder(); break;
                        case 1: CreateProfile(); break;
                        case 2: ModifyProfile(); break;
                        case 3: DeleteProfile(); break;
                        case 4: return;
                    }
                }
            }
        }

        // --- CREAZIONE E MODIFICA PROFILI ---
        static void CreateProfile()
        {
            Console.Clear();
            Console.Write("Inserisci il nome del nuovo profilo: ");
            Console.CursorVisible = true;
            string name = Console.ReadLine();
            Console.CursorVisible = false;

            if (string.IsNullOrWhiteSpace(name)) return;

            List<ScrcpyFlag> currentFlags = MasterFlagList.Select(f => f.Clone()).ToList();
            
            if (EditFlags(name, currentFlags))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(name + "|");
                foreach (var f in currentFlags)
                {
                    if (f.IsSelected)
                    {
                        sb.Append(f.Flag);
                        if (f.RequiresInput)
                        {
                            sb.Append("=" + f.UserInput);
                        }
                        sb.Append(" ");
                    }
                }

                List<string> profiles = LoadProfiles();
                profiles.Add(sb.ToString().Trim());
                SaveAllData(profiles);
            }
        }

        static void ModifyProfile()
        {
            List<string> profiles = LoadProfiles();
            if (profiles.Count == 0) return;

            int sel = SelectProfileFromList("Seleziona il profilo da modificare:");
            if (sel == -1) return;

            string[] parts = profiles[sel].Split('|');
            string name = parts[0];
            string args = parts.Length > 1 ? parts[1] : "";

            List<ScrcpyFlag> currentFlags = MasterFlagList.Select(f => f.Clone()).ToList();
            
            foreach (var flag in currentFlags)
            {
                if (args.Contains(flag.Flag))
                {
                    flag.IsSelected = true;
                    if (flag.RequiresInput)
                    {
                        string search = flag.Flag + "=";
                        int idx = args.IndexOf(search);
                        if (idx != -1)
                        {
                            int start = idx + search.Length;
                            int end = args.IndexOf(" --", start);
                            if (end == -1) end = args.Length;
                            flag.UserInput = args.Substring(start, end - start).Trim();
                        }
                    }
                }
            }

            if (EditFlags(name, currentFlags))
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(name + "|");
                foreach (var f in currentFlags)
                {
                    if (f.IsSelected)
                    {
                        sb.Append(f.Flag);
                        if (f.RequiresInput)
                        {
                            sb.Append("=" + f.UserInput);
                        }
                        sb.Append(" ");
                    }
                }

                profiles[sel] = sb.ToString().Trim();
                SaveAllData(profiles);
            }
        }

        static void DeleteProfile()
        {
            List<string> profiles = LoadProfiles();
            if (profiles.Count == 0) return;

            int sel = SelectProfileFromList("Seleziona il profilo da ELIMINARE:");
            if (sel == -1) return;

            Console.WriteLine("\nSei sicuro di voler eliminare '" + profiles[sel].Split('|')[0] + "'? (S/N)");
            ConsoleKeyInfo k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.S || k.Key == ConsoleKey.Y)
            {
                profiles.RemoveAt(sel);
                SaveAllData(profiles);
            }
        }

        static bool EditFlags(string profileName, List<ScrcpyFlag> flags)
        {
            int selectedIndex = 0;
            int scrollOffset = 0;
            int windowHeight = Console.WindowHeight - 8;

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("MODIFICA PROFILO: " + profileName);
                Console.ResetColor();
                Console.WriteLine("Usa FRECCE per scorrere, INVIO/X per attivare/modificare, ESC per salvare ed uscire.\n");

                if (selectedIndex < scrollOffset) scrollOffset = selectedIndex;
                if (selectedIndex >= scrollOffset + windowHeight) scrollOffset = selectedIndex - windowHeight + 1;

                for (int i = scrollOffset; i < Math.Min(scrollOffset + windowHeight, flags.Count); i++)
                {
                    var f = flags[i];
                    string check = f.IsSelected ? "[X]" : "[ ]";
                    string val = (f.IsSelected && f.RequiresInput) ? (" = " + f.UserInput) : "";
                    
                    if (i == selectedIndex)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.WriteLine(string.Format("{0} {1}{2}", check, f.Flag, val));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0} {1}{2}", check, f.Flag, val));
                    }
                }

                Console.SetCursorPosition(0, Console.WindowHeight - 4);
                Console.WriteLine(new string('-', Console.WindowWidth));
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("DESCRIZIONE:");
                Console.ResetColor();
                
                string desc = flags[selectedIndex].Description;
                if (desc.Length > Console.WindowWidth) desc = desc.Substring(0, Console.WindowWidth - 1);
                Console.WriteLine(desc);

                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.UpArrow)
                {
                    if (selectedIndex > 0) selectedIndex--;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    if (selectedIndex < flags.Count - 1) selectedIndex++;
                }
                else if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Spacebar || key.Key == ConsoleKey.X)
                {
                    var f = flags[selectedIndex];
                    if (f.RequiresInput)
                    {
                        // SE IL FLAG È --start-app, APRI IL SELETTORE
                        if (f.Flag == "--start-app")
                        {
                            string selectedApp = SelectAppFromDevice();
                            if (!string.IsNullOrEmpty(selectedApp))
                            {
                                f.UserInput = selectedApp;
                                f.IsSelected = true;
                            }
                            // Se torna null (ESC), non facciamo nulla (resta come prima)
                        }
                        else
                        {
                            // COMPORTAMENTO STANDARD PER GLI ALTRI FLAG
                            f.IsSelected = true;
                            Console.SetCursorPosition(0, Console.WindowHeight - 2);
                            Console.Write("Inserisci valore per " + f.Flag + ": ");
                            Console.CursorVisible = true;
                            string input = Console.ReadLine();
                            Console.CursorVisible = false;
                            if (!string.IsNullOrWhiteSpace(input))
                            {
                                f.UserInput = input;
                            }
                            else
                            {
                                f.IsSelected = false;
                            }
                        }
                    }
                    else
                    {
                        f.IsSelected = !f.IsSelected;
                    }
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    return true;
                }
            }
        }

        static int SelectProfileFromList(string title)
        {
            List<string> profiles = LoadProfiles();
            int idx = 0;
            while (true)
            {
                Console.Clear();
                Console.WriteLine(title);
                for (int i = 0; i < profiles.Count; i++)
                {
                    if (i == idx) Console.Write("> "); else Console.Write("  ");
                    Console.WriteLine(profiles[i].Split('|')[0]);
                }
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k.Key == ConsoleKey.UpArrow && idx > 0) idx--;
                if (k.Key == ConsoleKey.DownArrow && idx < profiles.Count - 1) idx++;
                if (k.Key == ConsoleKey.Enter) return idx;
                if (k.Key == ConsoleKey.Escape) return -1;
            }
        }

        // --- GESTIONE SELEZIONE APP (SHIFT CLICK & EDIT) ---

        static string InjectStartApp(string fullProfileLine, string packageName)
        {
            string[] parts = fullProfileLine.Split('|');
            string name = parts[0];
            string args = parts.Length > 1 ? parts[1] : "";

            string pattern = @"--start-app=[^\s]+";
            
            if (Regex.IsMatch(args, pattern))
            {
                args = Regex.Replace(args, pattern, "--start-app=" + packageName);
            }
            else
            {
                args = args + " --start-app=" + packageName;
            }

            return name + " (Custom App)|" + args;
        }

        // Struttura di supporto per l'ordinamento
        class PackageMatch
        {
            public string Name;
            public int Score; // 0 = contains, >0 = distance
        }

        static string SelectAppFromDevice()
        {
            Console.Clear();
            Console.WriteLine("Inizializzazione connessione ADB...");

            bool showSystemApps = false;
            // Carichiamo tutte le liste subito
            List<string> allApps = GetInstalledPackages(true);
            List<string> userApps = GetInstalledPackages(false);

            List<string> currentSource = userApps; // Default

            string searchBuffer = "";
            int idx = 0;
            int windowHeight = Console.WindowHeight - 8;
            int scrollOffset = 0;

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("SELEZIONA APP:");
                Console.ResetColor();

                if (showSystemApps)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[ SYSTEM + USER ]");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[ ONLY USER APPS ]");
                }
                
                Console.ResetColor();
                Console.Write("  (F1: Toggle) | (ESC: Annulla)\n");
                
                // Disegna barra di ricerca
                Console.Write("Cerca: ");
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(searchBuffer.PadRight(30));
                Console.ResetColor();
                Console.WriteLine("\n------------------------------------------------");

                // Filtro e Ordinamento (Fuzzy Logic)
                List<string> filteredList = new List<string>();

                if (string.IsNullOrEmpty(searchBuffer))
                {
                    filteredList = new List<string>(currentSource);
                }
                else
                {
                    string q = searchBuffer.ToLower();
                    List<PackageMatch> matches = new List<PackageMatch>();

                    foreach (var pkg in currentSource)
                    {
                        string pLower = pkg.ToLower();
                        int score = 1000;

                        // Priorità 1: Contiene la stringa esatta
                        if (pLower.Contains(q))
                        {
                            score = 0; 
                        }
                        else
                        {
                            // Priorità 2: Fuzzy search sui segmenti
                            string[] segments = pLower.Split('.');
                            foreach (var seg in segments)
                            {
                                int dist = ComputeLevenshteinDistance(q, seg);
                                if (dist < score) score = dist;
                            }
                        }

                        // Mostriamo solo se score è decente
                        if (score < 4) 
                        {
                            matches.Add(new PackageMatch { Name = pkg, Score = score });
                        }
                    }

                    // Ordiniamo
                    filteredList = matches.OrderBy(m => m.Score)
                                          .ThenBy(m => m.Name)
                                          .Select(m => m.Name)
                                          .ToList();
                }

                // Gestione lista vuota
                if (filteredList.Count == 0)
                {
                    Console.WriteLine(" Nessun risultato trovato.");
                }
                else
                {
                    // Gestione Indice e Scroll
                    if (idx >= filteredList.Count) idx = filteredList.Count - 1;
                    if (idx < 0) idx = 0;

                    if (idx < scrollOffset) scrollOffset = idx;
                    if (idx >= scrollOffset + windowHeight) scrollOffset = idx - windowHeight + 1;

                    int limit = Math.Min(scrollOffset + windowHeight, filteredList.Count);

                    for (int i = scrollOffset; i < limit; i++)
                    {
                        if (i == idx)
                        {
                            Console.BackgroundColor = ConsoleColor.Gray;
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.WriteLine("> " + filteredList[i]);
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine("  " + filteredList[i]);
                        }
                    }
                    Console.WriteLine("------------------------------------------------");
                    Console.WriteLine(string.Format("App: {0}/{1}", idx + 1, filteredList.Count));
                }

                ConsoleKeyInfo k = Console.ReadKey(true);
                
                if (k.Key == ConsoleKey.UpArrow)
                {
                    if (idx > 0) idx--;
                }
                else if (k.Key == ConsoleKey.DownArrow)
                {
                    if (filteredList.Count > 0 && idx < filteredList.Count - 1) idx++;
                }
                else if (k.Key == ConsoleKey.Enter || (string.IsNullOrEmpty(searchBuffer) && (k.Key == ConsoleKey.X || k.Key == ConsoleKey.Spacebar)))
                {
                    if (filteredList.Count > 0)
                        return filteredList[idx];
                }
                else if (k.Key == ConsoleKey.Escape)
                {
                    return null;
                }
                else if (k.Key == ConsoleKey.F1)
                {
                    showSystemApps = !showSystemApps;
                    currentSource = showSystemApps ? allApps : userApps;
                    idx = 0;
                    scrollOffset = 0;
                }
                else if (k.Key == ConsoleKey.Backspace)
                {
                    if (searchBuffer.Length > 0)
                    {
                        searchBuffer = searchBuffer.Substring(0, searchBuffer.Length - 1);
                        idx = 0;
                        scrollOffset = 0;
                    }
                }
                else if (!char.IsControl(k.KeyChar))
                {
                    searchBuffer += k.KeyChar;
                    idx = 0;
                    scrollOffset = 0;
                }
            }
        }

        static int ComputeLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        static List<string> GetInstalledPackages(bool includeSystem)
        {
            List<string> result = new List<string>();
            
            if (string.IsNullOrEmpty(ScrcpyFolder)) return result;
            string adbPath = Path.Combine(ScrcpyFolder, "adb.exe");
            
            if (!File.Exists(adbPath)) adbPath = "adb";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = adbPath;
                
                if (includeSystem)
                    psi.Arguments = "shell pm list packages";
                else
                    psi.Arguments = "shell pm list packages -3";

                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.StandardOutputEncoding = Encoding.UTF8;

                using (Process p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();

                    string[] lines = output.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("package:"))
                        {
                            string pkg = line.Substring(8).Trim();
                            if (!string.IsNullOrEmpty(pkg))
                            {
                                result.Add(pkg);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignora errori
            }

            result.Sort();
            return result;
        }

        // --- AVVIO SCRCPY ---

        static void LaunchScrcpy(string profileLine)
        {
            if (string.IsNullOrEmpty(ScrcpyFolder))
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERRORE: Non hai impostato la cartella di scrcpy!");
                Console.WriteLine("Vai su Opzioni -> Imposta cartella Scrcpy.");
                Console.ResetColor();
                Console.WriteLine("\nPremi un tasto per continuare...");
                Console.ReadKey();
                return;
            }

            string exePath = Path.Combine(ScrcpyFolder, "scrcpy.exe");
            if (!File.Exists(exePath))
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERRORE: scrcpy.exe non trovato in:");
                Console.WriteLine(ScrcpyFolder);
                Console.ResetColor();
                Console.WriteLine("\nPremi un tasto per continuare...");
                Console.ReadKey();
                return;
            }

            string args = "";
            if (profileLine.Contains("|"))
            {
                args = profileLine.Split('|')[1];
            }

            Console.Clear();
            Console.WriteLine("Avvio scrcpy...");
            Console.WriteLine("EXE: " + exePath);
            Console.WriteLine("ARG: " + args);
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exePath;
                psi.Arguments = args;
                psi.UseShellExecute = false; 
                psi.WorkingDirectory = ScrcpyFolder; 
                
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Errore nell'avvio di scrcpy: " + ex.Message);
                Console.ReadKey();
            }
        }

        // --- INIZIALIZZAZIONE LISTA FLAG ---
        static void InitializeFlags()
        {
            MasterFlagList.Add(new ScrcpyFlag("--always-on-top", "Mantiene la finestra sempre in primo piano."));
            MasterFlagList.Add(new ScrcpyFlag("--stay-awake", "Mantiene lo schermo del dispositivo acceso quando collegato.", false));
            MasterFlagList.Add(new ScrcpyFlag("--turn-screen-off", "Spegne lo schermo del dispositivo immediatamente all'avvio.", false));
            MasterFlagList.Add(new ScrcpyFlag("--fullscreen", "Avvia a schermo intero.", false));
            MasterFlagList.Add(new ScrcpyFlag("--no-control", "Disabilita il controllo (solo visualizzazione).", false));
            MasterFlagList.Add(new ScrcpyFlag("--no-audio", "Disabilita l'audio.", false));
            MasterFlagList.Add(new ScrcpyFlag("--no-video", "Disabilita il video (solo audio).", false));
            
            MasterFlagList.Add(new ScrcpyFlag("--max-fps", "Limita i frame per secondo (es. 30, 60).", true));
            MasterFlagList.Add(new ScrcpyFlag("--video-bit-rate", "Bitrate video (es. 4M, 8M). Default 8M.", true));
            MasterFlagList.Add(new ScrcpyFlag("--new-display", "Crea nuovo display (es. 1920x1080). Lascia vuoto per default.", true));
            MasterFlagList.Add(new ScrcpyFlag("--start-app", "Avvia una specifica app (nome pacchetto).", true));
            MasterFlagList.Add(new ScrcpyFlag("--max-size", "Limita larghezza/altezza (es. 1024).", true));
            MasterFlagList.Add(new ScrcpyFlag("--window-title", "Imposta un titolo personalizzato per la finestra.", true));
            MasterFlagList.Add(new ScrcpyFlag("--record", "Registra lo schermo su file (es. file.mp4).", true));
            MasterFlagList.Add(new ScrcpyFlag("--serial", "Specifica il seriale del dispositivo (se multipli).", true));
            MasterFlagList.Add(new ScrcpyFlag("--crop", "Ritaglia lo schermo (width:height:x:y).", true));
            
            MasterFlagList.Add(new ScrcpyFlag("--audio-bit-rate", "Bitrate audio (es. 128K).", true));
            MasterFlagList.Add(new ScrcpyFlag("--audio-buffer", "Buffer audio in ms (default 50).", true));
            MasterFlagList.Add(new ScrcpyFlag("--audio-source", "Sorgente audio (output, mic, playback).", true));
            MasterFlagList.Add(new ScrcpyFlag("--require-audio", "Fallisce se l'audio non funziona.", false));

            MasterFlagList.Add(new ScrcpyFlag("--video-source=camera", "Usa la fotocamera invece dello schermo.", false));
            MasterFlagList.Add(new ScrcpyFlag("--camera-id", "ID della fotocamera da usare.", true));
            MasterFlagList.Add(new ScrcpyFlag("--camera-size", "Dimensione cattura camera (es. 1920x1080).", true));
            MasterFlagList.Add(new ScrcpyFlag("--camera-facing", "Direzione camera (front, back).", true));

            MasterFlagList.Add(new ScrcpyFlag("--orientation", "Blocca orientamento (0, 90, 180, 270).", true));
            MasterFlagList.Add(new ScrcpyFlag("--display-id", "Specifica quale display specchiare.", true));
            MasterFlagList.Add(new ScrcpyFlag("--window-borderless", "Finestra senza bordi.", false));
            MasterFlagList.Add(new ScrcpyFlag("--window-x", "Posizione X finestra.", true));
            MasterFlagList.Add(new ScrcpyFlag("--window-y", "Posizione Y finestra.", true));
            MasterFlagList.Add(new ScrcpyFlag("--window-width", "Larghezza iniziale finestra.", true));
            MasterFlagList.Add(new ScrcpyFlag("--window-height", "Altezza iniziale finestra.", true));

            MasterFlagList.Add(new ScrcpyFlag("--show-touches", "Mostra i tocchi fisici sullo schermo.", false));
            MasterFlagList.Add(new ScrcpyFlag("--keyboard", "Modalità tastiera (disabled, uhid, aoa, sdk).", true));
            MasterFlagList.Add(new ScrcpyFlag("--mouse", "Modalità mouse (disabled, uhid, aoa, sdk).", true));
            MasterFlagList.Add(new ScrcpyFlag("--gamepad", "Modalità gamepad (disabled, uhid, aoa).", true));
            MasterFlagList.Add(new ScrcpyFlag("--prefer-text", "Inietta testo invece di eventi tasto (utile per caratteri speciali).", false));
            MasterFlagList.Add(new ScrcpyFlag("--raw-key-events", "Inietta tutti i tasti come eventi raw.", false));

            MasterFlagList.Add(new ScrcpyFlag("--tcpip", "Connetti via TCP/IP (opzionale: ip:port).", true));
            MasterFlagList.Add(new ScrcpyFlag("--select-usb", "Usa dispositivo USB.", false));
            MasterFlagList.Add(new ScrcpyFlag("--select-tcpip", "Usa dispositivo TCP/IP.", false));
            MasterFlagList.Add(new ScrcpyFlag("--tunnel-host", "IP tunnel ADB.", true));
            MasterFlagList.Add(new ScrcpyFlag("--tunnel-port", "Porta tunnel ADB.", true));

            MasterFlagList.Add(new ScrcpyFlag("--disable-screensaver", "Disabilita screensaver PC.", false));
            MasterFlagList.Add(new ScrcpyFlag("--no-clipboard-autosync", "Disabilita sync appunti.", false));
            MasterFlagList.Add(new ScrcpyFlag("--no-key-repeat", "Non ripetere tasti tenuti premuti.", false));
            MasterFlagList.Add(new ScrcpyFlag("--no-vd-destroy-content", "Non distruggere contenuto display virtuale alla chiusura.", false));
            MasterFlagList.Add(new ScrcpyFlag("--power-off-on-close", "Spegni schermo dispositivo alla chiusura.", false));
            MasterFlagList.Add(new ScrcpyFlag("--kill-adb-on-close", "Uccidi processo ADB alla chiusura.", false));
            MasterFlagList.Add(new ScrcpyFlag("--print-fps", "Stampa FPS nella console.", false));
            MasterFlagList.Add(new ScrcpyFlag("--verbosity", "Livello log (verbose, debug, info, warn, error).", true));
        }
    }
}