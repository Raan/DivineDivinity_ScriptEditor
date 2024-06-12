using Microsoft.VisualBasic.Logging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace StoryEditor
{
    public partial class Form1 : Form
    {
        public string pathToStoryFile = "";
        public string pathToStoryBinFile = "story.000";
        public string pathToDivFile = "";
        public string pathToGameFolder = "";
        public string pathToMainProgramFolder = "";
        public string[]? Story;
        public List<Objects> objects = new List<Objects>();
        public List<Goal> goals = new List<Goal>();
        public List<string> subGoals = new List<string>();
        private string stringFilter = "";
        public int selectedGoal = -1;
        public bool compile_trace = false;
        public bool debug_trace = false;
        public bool build_and_run_game = false;
        string storyVersion = "";
        public Form1()
        {
            InitializeComponent();
            pathToMainProgramFolder = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists("config.ini"))
            {
                string[] lines = File.ReadLines("config.ini").ToArray();
                if (lines.Length > 2 && File.Exists(lines[0]))
                {
                    pathToDivFile = lines[0];
                    pathToGameFolder = lines[1];
                    pathToStoryBinFile = lines[2];
                }
                else
                {
                    OpenFileDialog Div = new OpenFileDialog();
                    Div.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    Div.Filter = "Div.exe (*.exe)|*.exe|All files (*.*)|*.*";
                    if (Div.ShowDialog() == DialogResult.OK)
                    {
                        pathToDivFile = Div.FileName;
                        int x = pathToDivFile.LastIndexOf("\\", StringComparison.CurrentCulture);
                        pathToGameFolder = pathToDivFile.Remove(x);
                        pathToStoryBinFile = pathToDivFile.Remove(x) + "\\main\\startup\\story.000";

                        lines = new string[3];
                        lines[0] = pathToDivFile;
                        lines[1] = pathToGameFolder;
                        lines[2] = pathToStoryBinFile;
                        File.WriteAllLines("config.ini", lines);
                    }
                }
            }
            else
            {
                if (pathToDivFile == "")
                {
                    OpenFileDialog Div = new OpenFileDialog();
                    Div.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    Div.Filter = "Div.exe (*.exe)|*.exe|All files (*.*)|*.*";
                    if (Div.ShowDialog() == DialogResult.OK)
                    {
                        pathToDivFile = Div.FileName;
                        int x = pathToDivFile.LastIndexOf("\\", StringComparison.CurrentCulture);
                        pathToGameFolder = pathToDivFile.Remove(x);
                        pathToStoryBinFile = pathToDivFile.Remove(x) + "\\main\\startup\\story.000";

                        String[] lines = new string[3];
                        lines[0] = pathToDivFile;
                        lines[1] = pathToGameFolder;
                        lines[2] = pathToStoryBinFile;
                        File.WriteAllLines("config.ini", lines);
                        pathToDivFile = Div.FileName;
                    }
                }
            }
            ConsoleRichTextBox.AppendText("Game directory: " + pathToGameFolder + "\n");
            ConsoleRichTextBox.AppendText("Path to div.exe: " + pathToDivFile + "\n");
            ConsoleRichTextBox.AppendText("Path to story.000: " + pathToStoryBinFile + "\n");
            if (File.Exists(pathToStoryFile))
            {
                Story = System.IO.File.ReadLines(pathToStoryFile).ToArray();
                pathToStoryFile = "Story.div";
                StoryUnpack(Story);
            }
        }
        private void StoryUnpack(string[] story)
        {
            objects.Clear();
            goals.Clear();
            subGoals.Clear();

            bool INIT = false;
            bool KB = false;
            bool EXIT = false;
            ConsoleRichTextBox.Text = "";
            foreach (string storyLine in story)
            {
                string[] words = storyLine.Split(new char[] { ' ' });
                if (words[0] == "object" && words[1] == "{")
                {
                    objects.Add(new Objects(
                        words[2].Remove(words[2].Length - 1),
                        words[3].Remove(words[3].Length - 1),
                        words[6].Remove(words[6].Length - 1)));
                }
                if (storyLine.Length > 15 && storyLine.Remove(5) == "Goal(")
                {
                    INIT = false;
                    KB = false;
                    EXIT = false;
                    words = storyLine.Split(new char[] { '(' });
                    if (words[1].Remove(0, words[1].Length - 5) == "Title")
                    {
                        string name = words[2].Remove(0, 1);
                        name = name.Remove(name.Length - 3);
                        if (Int32.TryParse(words[1].Remove(5), out int x))
                        {
                            goals.Add(new Goal(x, name));
                        }
                        else
                        {
                            if (Int32.TryParse(words[1].Remove(4), out x))
                            {
                                goals.Add(new Goal(x, name));
                            }
                            else
                            {
                                if (Int32.TryParse(words[1].Remove(3), out x))
                                {
                                    goals.Add(new Goal(x, name));
                                }
                                else
                                {
                                    if (Int32.TryParse(words[1].Remove(2), out x))
                                    {
                                        goals.Add(new Goal(x, name));
                                    }
                                    else
                                    {
                                        if (Int32.TryParse(words[1].Remove(1), out x))
                                        {
                                            goals.Add(new Goal(x, name));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (storyLine == "}")
                {
                    INIT = false;
                    KB = false;
                    EXIT = false;
                }
                if (storyLine == "INIT {")
                {
                    INIT = true;
                    KB = false;
                    EXIT = false;
                }
                if (storyLine == "KB {")
                {
                    INIT = false;
                    KB = true;
                    EXIT = false;
                }
                if (storyLine == "EXIT {")
                {
                    INIT = false;
                    KB = false;
                    EXIT = true;
                }
                if (INIT && storyLine != "INIT {")
                {
                    goals[^1].INIT.Add(storyLine);
                }
                if (KB && storyLine != "KB {")
                {
                    goals[^1].KB.Add(storyLine);
                }
                if (EXIT && storyLine != "EXIT {")
                {
                    goals[^1].EXIT.Add(storyLine);
                }
                // Story version
                if (storyLine.Contains("version \"", StringComparison.CurrentCulture))
                {
                    storyVersion = storyLine;
                    ConsoleRichTextBox.AppendText("Story " + storyLine + "\n");
                }
                // SubGoals
                words = storyLine.Split(new char[] { '.' });
                if (words.Length > 1 && words[1].Contains("SubGoal", StringComparison.CurrentCulture))
                {
                    subGoals.Add(storyLine);
                }
            }
            UpdateObjects();
            ConsoleRichTextBox.AppendText(pathToStoryFile + " unpack" + "\n");
            GoalListBox.SelectedIndex = 0;
            selectedGoal = 0;
        }
        private void SaveStory(
            string patch,
            bool C_trace,
            bool D_trace,
            List<Objects> O,
            List<Goal> G,
            List<string> SG,
            string ver)
        {
            if (selectedGoal >= 0)
            {
                goals[selectedGoal].INIT.Clear();
                foreach (var line in INITRichTextBox.Lines)
                {
                    goals[selectedGoal].INIT.Add(line);
                }
                goals[selectedGoal].KB.Clear();
                foreach (var line in KBRichTextBox.Lines)
                {
                    goals[selectedGoal].KB.Add(line);
                }
                goals[selectedGoal].EXIT.Clear();
                foreach (var line in EXITRichTextBox.Lines)
                {
                    goals[selectedGoal].EXIT.Add(line);
                }
            }

            TextWriter tw = new StreamWriter(patch, false);
            if (C_trace)
            {
                tw.WriteLine("option compile_trace");
            }
            else
            {
                tw.WriteLine("// option compile_trace");
            }
            if (D_trace)
            {
                tw.WriteLine("option debug_trace");
            }
            else
            {
                tw.WriteLine("// option debug_trace");
            }
            tw.WriteLine("");
            tw.WriteLine("type { NPC, 4 }");
            tw.WriteLine("type { OBJECT, 5 }");
            tw.WriteLine("type { DIALOG, 6 }");
            tw.WriteLine("type { REGION, 7 }");
            tw.WriteLine("type { LOCATION, 8 }");
            tw.WriteLine("type { NPC_CLASS, 9 }");
            tw.WriteLine("type { OBJECT_CLASS, 10 }");
            tw.WriteLine("type { DIALOG_EVENT, 11 }");
            tw.WriteLine("type { ENGINE, 12 }");
            tw.WriteLine("type { FUNCTION, 13 }");
            tw.WriteLine("type { SREGION, 15 }");
            tw.WriteLine("");

            foreach (var o in O)
            {
                tw.WriteLine("object { " + o.name + ", " + o.type + ", ( " + o.type + ", " + o.ID + ", 0, 0 ) }");
            }
            tw.WriteLine("");

            if (File.Exists("instruct.000"))
            {
                string[] inst = System.IO.File.ReadLines("instruct.000").ToArray();
                foreach (var i in inst)
                {
                    tw.WriteLine(i);
                }
            }

            tw.WriteLine("");
            tw.WriteLine(ver);
            tw.WriteLine("");
            foreach (var g in G)
            {
                tw.WriteLine("Goal(" + g.ID + ").Title(\"" + g.NAME + "\");");
                tw.WriteLine("Goal(" + g.ID + ") {");
                tw.WriteLine("INIT {");
                foreach (var i in g.INIT)
                {
                    tw.WriteLine(i);
                }
                tw.WriteLine("}");
                tw.WriteLine("");
                tw.WriteLine("KB {");
                foreach (var k in g.KB)
                {
                    tw.WriteLine(k);
                }
                tw.WriteLine("}");
                tw.WriteLine("");
                tw.WriteLine("EXIT {");
                foreach (var e in g.EXIT)
                {
                    tw.WriteLine(e);
                }
                tw.WriteLine("}");
                tw.WriteLine("");
                tw.WriteLine("}");
                tw.WriteLine("");
            }
            foreach (var s in SG)
            {
                tw.WriteLine(s);
            }
            tw.Close();
            ConsoleRichTextBox.AppendText(patch + " save" + "\n");
        }
        private void UpdateObjects()
        {
            NPCListBox.Items.Clear();
            OBJECTListBox.Items.Clear();
            DIALOGListBox.Items.Clear();
            REGIONListBox.Items.Clear();
            LOCATIONListBox.Items.Clear();
            NPC_CLASSListBox.Items.Clear();
            OBJECT_CLASSListBox.Items.Clear();
            DIALOG_EVENTListBox.Items.Clear();
            ENGINEListBox.Items.Clear();
            FUNCTIONListBox.Items.Clear();
            SREGIONListBox.Items.Clear();
            GoalListBox.Items.Clear();

            objects.Sort((x, y) => x.ID.CompareTo(y.ID));
            foreach (var obj in objects)
            {
                if (obj.type == 4) NPCListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 5) OBJECTListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 6) DIALOGListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 7) REGIONListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 8) LOCATIONListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 9) NPC_CLASSListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 10) OBJECT_CLASSListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 11) DIALOG_EVENTListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 12) ENGINEListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 13) FUNCTIONListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);
                if (obj.type == 15) SREGIONListBox.Items.Add(obj.ID.ToString().PadRight(6, ' ') + obj.name);

            }
            // Заполняем список Goal stringFilter
            foreach (var g in goals)
            {
                if (stringFilter.Length == 0)
                {
                    GoalListBox.Items.Add(g.ID.ToString().PadRight(6, ' ') + g.NAME);
                }
                else
                {
                    if (stringFilter.Length <= g.NAME.Length && stringFilter.Length > 0)
                    {
                        if (g.NAME.Contains(stringFilter, StringComparison.CurrentCultureIgnoreCase))
                        {
                            GoalListBox.Items.Add(g.ID.ToString().PadRight(6, ' ') + g.NAME);
                        }
                    }
                }

            }
        }
        static void HighlightPhrase(RichTextBox box, string phrase, Color color)
        {
            int pos = box.SelectionStart;
            string s = box.Text;
            for (int ix = 0; ;)
            {
                int jx = s.IndexOf(phrase, ix, StringComparison.CurrentCulture);
                if (jx < 0) break;
                int a = s.IndexOf("\n", jx);
                if (jx + phrase.Length == a)
                {
                    box.SelectionStart = jx;
                    box.SelectionLength = phrase.Length;
                    box.SelectionColor = color;
                }
                ix = jx + 1;
            }
            box.SelectionStart = pos;
            box.SelectionLength = 0;
        }
        private void GoalListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            INITRichTextBox.Text = "";
            KBRichTextBox.Text = "";
            EXITRichTextBox.Text = "";
            string buffer = "";
            string[] words = ("" + GoalListBox.SelectedItem).Split(new char[] { ' ' });
            selectedGoal = Int32.Parse(words[0]) - 1;
            foreach (var s in goals[selectedGoal].INIT)
            {
                buffer += s + System.Environment.NewLine;
            }
            INITRichTextBox.Text += buffer;
            buffer = "";
            foreach (var s in goals[selectedGoal].KB)
            {
                buffer += s + System.Environment.NewLine;
            }
            KBRichTextBox.Text += buffer;
            buffer = "";
            foreach (var s in goals[selectedGoal].EXIT)
            {
                buffer += s + System.Environment.NewLine;
            }
            EXITRichTextBox.Text += buffer;
            buffer = "";

            ColoredWords();
        }
        private void ColoredWords()
        {
            HighlightPhrase(KBRichTextBox, "IF", Color.Orange);
            HighlightPhrase(KBRichTextBox, "AND", Color.Orange);
            HighlightPhrase(KBRichTextBox, "NOT", Color.Orange);
            HighlightPhrase(KBRichTextBox, "AND NOT", Color.Orange);
            HighlightPhrase(KBRichTextBox, "THEN", Color.Orange);
            HighlightPhrase(KBRichTextBox, "PROC", Color.Orange);
            HighlightPhrase(KBRichTextBox, "GoalCompleted;", Color.BlueViolet);
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            stringFilter = stringFilterTextBox.Text;
            UpdateObjects();
        }

        private void ClearFilterButton_Click(object sender, EventArgs e)
        {
            stringFilter = "";
            stringFilterTextBox.Clear();
            UpdateObjects();
        }

        private void INITRichTextBox_Leave(object sender, EventArgs e)
        {
            if (selectedGoal >= 0)
            {
                ColoredWords();
                goals[selectedGoal].INIT.Clear();
                foreach (var line in INITRichTextBox.Lines)
                {
                    goals[selectedGoal].INIT.Add(line);
                }
            }
        }

        private void KBRichTextBox_Leave(object sender, EventArgs e)
        {
            if (selectedGoal >= 0)
            {
                ColoredWords();
                goals[selectedGoal].KB.Clear();
                foreach (var line in KBRichTextBox.Lines)
                {
                    goals[selectedGoal].KB.Add(line);
                }
            }
        }

        private void EXITRichTextBox_Leave(object sender, EventArgs e)
        {
            if (selectedGoal >= 0)
            {
                ColoredWords();
                goals[selectedGoal].EXIT.Clear();
                foreach (var line in EXITRichTextBox.Lines)
                {
                    goals[selectedGoal].EXIT.Add(line);
                }
            }
        }

        private void compiletraceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            compile_trace = !compile_trace;
            compiletraceToolStripMenuItem.Checked = compile_trace;
        }

        private void debugtraceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debug_trace = !debug_trace;
            debugtraceToolStripMenuItem.Checked = debug_trace;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveStory(pathToStoryFile, compile_trace, debug_trace, objects, goals, subGoals, storyVersion);
        }

        private void BuildButton_Click(object sender, EventArgs e) // Сохраняем и компилируем бинарник
        {

            if(selectedGoal >= 0)
            {
                bool ready = true;
                if (pathToStoryFile == "")
                {
                    SaveFileDialog SF = new SaveFileDialog();
                    SF.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    SF.Filter = "Divinity Scripts(*.div)|*.div|All files(*.*)|*.*";
                    if (SF.ShowDialog() == DialogResult.OK)
                    {
                        pathToStoryFile = SF.FileName;
                        SaveStory(pathToStoryFile, compile_trace, debug_trace, objects, goals, subGoals, storyVersion);
                    }
                    else 
                    {
                        ready = false;
                    }
                }
                else
                {
                    SaveStory(pathToStoryFile, compile_trace, debug_trace, objects, goals, subGoals, storyVersion);
                }
                if (ready)
                {
                    if (build_and_run_game) // Запускаем игру
                    {
                        Environment.CurrentDirectory = pathToMainProgramFolder;
                        System.Diagnostics.Debug.WriteLine("1  " + Environment.CurrentDirectory);
                        ProcessStartInfo BuildStory = new()
                        {
                            WorkingDirectory = pathToMainProgramFolder,
                            FileName = @"OsirisCC.exe",
                            Arguments = " -log CON -compile \"" + pathToStoryFile + "\" -save \"" + pathToStoryBinFile + "\"",
                            UseShellExecute = true
                        };

                        Process? build = Process.Start(BuildStory);
                        build?.WaitForExit();
                        ConsoleRichTextBox.AppendText(pathToStoryBinFile + " build" + "\n");
                        Environment.CurrentDirectory = pathToGameFolder;
                        System.Diagnostics.Debug.WriteLine("2  " + Environment.CurrentDirectory);
                        ProcessStartInfo GameStart = new()
                        {
                            WorkingDirectory = pathToGameFolder,
                            FileName = pathToDivFile,
                            UseShellExecute = true
                        };
                        Process? game = Process.Start(GameStart);
                        game?.WaitForExit();
                        ConsoleRichTextBox.AppendText("Game started" + "\n");
                        Environment.CurrentDirectory = pathToMainProgramFolder;
                    }
                    else
                    {
                        Environment.CurrentDirectory = pathToMainProgramFolder;
                        System.Diagnostics.Debug.WriteLine("3  " + Environment.CurrentDirectory);
                        ProcessStartInfo BuildStory = new()
                        {
                            WorkingDirectory = pathToMainProgramFolder,
                            FileName = @"OsirisCC.exe",
                            Arguments = " -log CON -compile \"" + pathToStoryFile + "\" -save \"" + pathToStoryBinFile + "\"",
                            UseShellExecute = true
                        };
                        Process? build = Process.Start(BuildStory);
                        build?.WaitForExit();
                        ConsoleRichTextBox.AppendText(pathToStoryBinFile + " build" + "\n");
                    }
                }
            }
        }
        public class Objects
        {
            public string name = "";
            public int type;
            public int ID;
            public Objects(string name, string type, string ID)
            {
                this.name = name;
                this.type = Int32.Parse(type);
                this.ID = Int32.Parse(ID);
            }
        }
        public class Goal
        {
            public int ID;
            public string NAME = "";
            public List<string> INIT = new List<string>();
            public List<string> KB = new List<string>();
            public List<string> EXIT = new List<string>();
            public Goal(int id, string name)
            {
                this.ID = id;
                this.NAME = name;
            }
        }
        private void buildAndRunGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            build_and_run_game = !build_and_run_game;
            buildAndRunGameToolStripMenuItem.Checked = build_and_run_game;
        }
        private void originalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (File.Exists("StoryOriginal.000"))
            {
                Story = System.IO.File.ReadLines("StoryOriginal.000").ToArray();
                StoryUnpack(Story);
            }
            else
            {
                ConsoleRichTextBox.AppendText("File StoryOriginal.000 not found" + "\n");
            }
            pathToStoryFile = "";
        }
        private void customToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OPF = new OpenFileDialog();
            OPF.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            OPF.Filter = "story.div (*.div)|*.div|All files (*.*)|*.*";
            if (OPF.ShowDialog() == DialogResult.OK)
            {
                pathToStoryFile = OPF.FileName;
                ConsoleRichTextBox.AppendText("Open file: " + pathToStoryFile + "\n");
                Story = System.IO.File.ReadLines(pathToStoryFile).ToArray();
                StoryUnpack(Story);
            }
        }
        private void saveStoryAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog SF = new SaveFileDialog();
            SF.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            SF.Filter = "Divinity Scripts(*.div)|*.div|All files(*.*)|*.*";
            if (SF.ShowDialog() == DialogResult.OK)
            {
                pathToStoryFile = SF.FileName;
                SaveStory(pathToStoryFile, compile_trace, debug_trace, objects, goals, subGoals, storyVersion);
            }
        }
        private void saveStoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pathToStoryFile == "")
            {
                SaveFileDialog SF = new SaveFileDialog();
                SF.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
                SF.Filter = "Divinity Scripts(*.div)|*.div|All files(*.*)|*.*";
                if (SF.ShowDialog() == DialogResult.OK)
                {
                    pathToStoryFile = SF.FileName;
                    SaveStory(pathToStoryFile, compile_trace, debug_trace, objects, goals, subGoals, storyVersion);
                }
            }
            else
            {
                SaveStory(pathToStoryFile, compile_trace, debug_trace, objects, goals, subGoals, storyVersion);
            }
        }
    }
}