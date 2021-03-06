﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DtronixModeler.Xaml
{
    public static class Commands
    {
        public static readonly RoutedUICommand NewDatabase = new RoutedUICommand("New Database", "NewDatabase", typeof(Commands));
        public static readonly RoutedUICommand ImportDatabase = new RoutedUICommand("Import Database", "ImportDatabase", typeof(Commands));
        public static readonly ICommand ImportMySqlMwb = new RoutedUICommand("Import MySql Mwb", "ImportMySqlMwb", typeof(Commands));
        public static readonly RoutedUICommand CloseDatabase = new RoutedUICommand("Close Database", "CloseDatabase", typeof(Commands));
        public static readonly RoutedUICommand OpenDatabaseDirectory = new RoutedUICommand("Open Containing Directory", "OpenDatabaseDirectory", typeof(Commands));
        public static readonly RoutedUICommand RenameDatabaseItem = new RoutedUICommand("Rename", "RenameDatabaseItem", typeof(Commands));
        public static readonly RoutedUICommand GenerateAll = new RoutedUICommand("Generate All", "GenerateAll", typeof(Commands));
        public static readonly RoutedUICommand Exit = new RoutedUICommand("Exit", "Exit", typeof(Commands));
        public static readonly RoutedUICommand About = new RoutedUICommand("About", "About", typeof(Commands));
        public static readonly RoutedUICommand MoveUp = new RoutedUICommand("Move Up", "MoveUp", typeof(Commands));
        public static readonly RoutedUICommand MoveDown = new RoutedUICommand("Move Down", "MoveDown", typeof(Commands));

        static Commands()
        {
            Exit.InputGestures.Add(new KeyGesture(Key.F4, ModifierKeys.Alt));
        }

        //public static readonly RoutedUICommand SomeOtherAction = new RoutedUICommand("Some other action", "SomeOtherAction", typeof(Window1));
        //public static readonly RoutedUICommand MoreDeeds = new RoutedUICommand("More deeds", "MoreDeeeds", typeof(Window1));


    }
}
