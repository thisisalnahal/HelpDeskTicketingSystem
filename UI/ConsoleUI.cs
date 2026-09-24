namespace HelpDeskTicketingSystem.UI
{
    internal class ConsoleUI
    {
        private const int Width = 100;

        public static void PrintHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('=', Width));
            int padding = (Width - title.Length) / 2;
            Console.WriteLine(new string(' ', Math.Max(padding, 0)) + title);
            Console.WriteLine(new string('=', Width));
            Console.ResetColor();
        }

        public static void PrintDivider()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('-', Width));
            Console.ResetColor();
        }

        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  [OK] " + message);
            Console.ResetColor();
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  [ERROR] " + message);
            Console.ResetColor();
        }

        public static void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  [WARNING] " + message);
            Console.ResetColor();
        }

        public static void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  " + message);
            Console.ResetColor();
        }

        public static void PrintLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void Print(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(message);
            Console.ResetColor();
        }

        public static int PrintMenu(string title, string[] options)
        {
            PrintHeader(title);
            for (int i = 0; i < options.Length; i++)
            {
                PrintInfo($"{i + 1} - {options[i]}");
            }

            int choice = 0;
            while (true)
            {
                Print("Select an option: ");
                int.TryParse(Console.ReadLine(), out choice);
                if (choice >= 1 && choice <= options.Length)
                    break;
                else
                    PrintError("Invalid Option. Try again!");
            }
            return choice;
        }

        public static void PrintTable(string[] headers, List<string[]> rows, ConsoleColor[]? rowColors = null)
        {
            int[] colWidths = new int[headers.Length];
            for (int i = 0; i < headers.Length; i++)
                colWidths[i] = headers[i].Length;

            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i].Length > colWidths[i])
                        colWidths[i] = row[i].Length;
                }
            }

            PrintRow(headers, colWidths, ConsoleColor.Cyan);
            Console.WriteLine(new string('-', SumWidths(colWidths) + (colWidths.Length-1) * 2));

            for (int i = 0; i < rows.Count; i++)
            {
                ConsoleColor color = ConsoleColor.White;
                if (rowColors != null && i < rowColors.Length)
                    color = rowColors[i];

                PrintRow(rows[i], colWidths, color);
            }
        }

        private static void PrintRow(string[] cells, int[] colWidths, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            for (int i = 0; i < cells.Length; i++)
            {
                Console.Write(cells[i].PadRight(colWidths[i] + 2));
            }
            Console.WriteLine();
            Console.ResetColor();
        }

        private static int SumWidths(int[] widths)
        {
            int total = 0;
            foreach (var w in widths) total += w;
            return total;
        }

        public static string ReadName(string prompt)
        {
            while (true)
            {
                Print(prompt);

                string input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input))
                    return input;

                PrintError("Name cannot be empty");
            }
        }

        public static string ReadEmail(string prompt)
        {
            while (true)
            {
                Print(prompt);

                string input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input) && input.Contains("@"))
                    return input;

                PrintError("Must contain @");
            }
        }

        public static string ReadPassword(string prompt)
        {
            while (true)
            {
                Print(prompt);

                string input = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(input) && input.Length >= 8)
                    return input;

                PrintError("Password should be greater than  or equal to 8 digits.");
            }
        }
        public static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
            Console.ResetColor();
        }

        public static bool Confirm(string prompt)
        {
            while (true)
            {
                Print(prompt + " (y/n): ");
                string input = Console.ReadLine();

                if (input != null && input.Trim().ToLower() == "y") return true;
                if (input != null && input.Trim().ToLower() == "n") return false;

                PrintError("Please enter y or n.");
            }
        }

        public static void ClearScreen()
        {
            Console.Clear();
        }
    }
}
