// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    [oldmsgpack::MessagePack.MessagePackObject(true)]
    [newmsgpack::MessagePack.MessagePackObject(true)]
    public class StringKeySerializerTarget2
    {
        public int TotalQuestions { get; set; }

        public int TotalUnanswered { get; set; }

        public int QuestionsPerMinute { get; set; }

        public int AnswersPerMinute { get; set; }

        public int TotalVotes { get; set; }

        public int BadgesPerMinute { get; set; }

        public int NewActiveUsers { get; set; }

        public int ApiRevision { get; set; }

        public int Site { get; set; }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

