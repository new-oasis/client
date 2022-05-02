// using System;
// using System.Linq;
// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection.Emit;
// using UnityEngine;
// using Unity.Mathematics;
// using Unity.Entities;
// using Grpc.Core;
// using Oasis;
// // using Oasis.Game;
// using Oasis.Core;
// using Oasis.ECS;
// using NUnit.Framework;
// using UnityEngine.TestTools;
//
//
// public class StructTests
// {
//     private bool sceneLoaded;
//     private EntityManager em;
//
//     [OneTimeSetUp]
//     public void OneTimeSetup()
//     {
//         Debug.Log("One Time Setup");
//     }
//
//
//     [Test]
//     public void BlockStateEqualsJustBlock()
//     {
//         Assert.AreEqual(
//             new BlockState {block = "one"},
//             new BlockState {block = "one"}
//         );
//         Assert.AreNotEqual(
//             new BlockState {block = "one"},
//             new BlockState {block = "two"}
//         );
//     }
//
//
//     [Test]
//     public void BlockStateEqualsBlockProperties()
//     {
//         Assert.AreEqual(
//             new BlockState{block = "one", properties = new Dictionary<string, string>()},
//             new BlockState{block = "one", properties = new Dictionary<string, string>()}
//         );
//         Assert.AreEqual(
//             new BlockState{block = "one", properties = new Dictionary<string, string>(){{"a","a"}}},
//             new BlockState{block = "one", properties = new Dictionary<string, string>(){{"a","a"}}}
//         );
//         Assert.AreNotEqual(
//             new BlockState{block = "one", properties = new Dictionary<string, string>(){{"a","a"}}},
//             new BlockState{block = "one", properties = new Dictionary<string, string>(){{"a","b"}}}
//         );
//     }
//
//     [Test]
//     public void BlockStatesDictionaryContainsKey()
//     {
//         Dictionary<BlockState, bool> loaded = new Dictionary<BlockState, bool>();
//         var orig = new BlockState {block = "one", properties = new Dictionary<string, string>() {{"on", "true"}}};
//         loaded[orig] = true;
//         
//         var dupe = new BlockState {block = "one", properties = new Dictionary<string, string>() {{"on", "true"}}};
//         Assert.IsTrue( loaded.ContainsKey(dupe) );
//         
//         var dupe2 = new BlockState {block = "one", properties = new Dictionary<string, string>() {{"on", "false"}}};
//         Assert.IsFalse( loaded.ContainsKey(dupe2) );
//     }
//
//
//
// }
//
