﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SonOfRobin
{
    public struct HintMessage
    {
        public enum BoxType { Dialogue, GreenBox, BlueBox, LightBlueBox, RedBox }

        public readonly string text;
        public readonly List<Texture2D> imageList;
        public readonly BoxType boxType;
        public readonly int delay;
        public readonly bool fieldOnly;
        public readonly bool blockInput;

        public HintMessage(string text, int delay = 1, bool fieldOnly = false, bool blockInput = false, List<Texture2D> imageList = null, BoxType boxType = BoxType.Dialogue)
        {
            this.text = text;
            this.imageList = imageList == null ? new List<Texture2D>() : imageList;
            this.boxType = boxType;
            this.delay = delay;
            this.fieldOnly = fieldOnly;
            this.blockInput = blockInput;

            this.ValidateImagesCount();
        }

        private void ValidateImagesCount()
        {
            MatchCollection matches = Regex.Matches(this.text, $@"\{TextWindow.imageMarkerStart}"); // $@ is needed for "\" character inside interpolated string

            if (this.imageList.Count != matches.Count) throw new ArgumentException($"HintMessage - count of markers ({matches.Count}) and images ({this.imageList.Count}) does not match.\n{this.text}");
        }

        public Scheduler.Task ConvertToTask(bool useTransitionOpen = false, bool useTransitionClose = false)
        {
            Color bgColor, textColor;

            switch (this.boxType)
            {
                case BoxType.Dialogue:
                    bgColor = Color.White;
                    textColor = Color.Black;
                    break;

                case BoxType.GreenBox:
                    bgColor = Color.Green;
                    textColor = Color.White;
                    break;

                case BoxType.BlueBox:
                    bgColor = Color.Blue;
                    textColor = Color.White;
                    break;

                case BoxType.LightBlueBox:
                    bgColor = Color.DodgerBlue;
                    textColor = Color.White;
                    break;

                case BoxType.RedBox:
                    bgColor = Color.DarkRed;
                    textColor = Color.White;
                    break;

                default:
                    { throw new DivideByZeroException($"Unsupported hint boxType - {boxType}."); }
            }

            var textWindowData = new Dictionary<string, Object> {
                { "text", this.text },
                { "imageList", this.imageList },
                { "bgColor", new List<byte> {bgColor.R, bgColor.G, bgColor.B } },
                { "textColor", new List<byte> {textColor.R, textColor.G, textColor.B }  },
                { "checkForDuplicate", true },
                { "useTransition", false },
                { "useTransitionOpen", useTransitionOpen },
                { "useTransitionClose", useTransitionClose },
                { "blocksUpdatesBelow", false },
                { "blockInputDuration", this.blockInput ? HintEngine.blockInputDuration : 0}
            };

            return new Scheduler.Task(taskName: Scheduler.TaskName.OpenTextWindow, turnOffInputUntilExecution: true, delay: this.delay, executeHelper: textWindowData, storeForLaterUse: true);
        }

        public static List<Object> ConvertToTasks(List<HintMessage> messageList)
        {
            var taskChain = new List<Object> { };

            int counter = 0;
            bool useTransitionOpen, useTransitionClose;
            foreach (HintMessage message in messageList)
            {
                useTransitionOpen = counter == 0;
                useTransitionClose = counter + 1 == messageList.Count;

                taskChain.Add(message.ConvertToTask(useTransitionOpen: useTransitionOpen, useTransitionClose: useTransitionClose));

                counter++;
            }

            return taskChain;
        }
    }
}
