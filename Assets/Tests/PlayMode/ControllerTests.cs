// using System.Collections;
// using UnityEngine;
// using Unity.Entities;
// using NUnit.Framework;
// using System.Threading.Tasks;
// using Oasis.Core;
// using UnityEngine.SceneManagement;
// using UnityEngine.TestTools;
// using Entity = Unity.Entities.Entity;
//
//
// public class ControllerTests
// {
//     private bool sceneLoaded;
//     private EntityManager em;
//
//     [OneTimeSetUp]
//     public void OneTimeSetup()
//     {
//         Debug.Log("One Time Setup");
//         DefaultWorldInitialization.DefaultLazyEditModeInitialize();
//         em = World.DefaultGameObjectInjectionWorld.EntityManager;
//         SceneManager.sceneLoaded += OnSceneLoaded;
//         SceneManager.LoadScene("Tests/PlayMode", LoadSceneMode.Single);
//     }
//
//     void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//     {
//         sceneLoaded = true;
//         Debug.Log("Scene Loaded");
//     }
//
//     [UnityTest]
//     public IEnumerator BlockFind()
//     {
//         yield return new WaitWhile(() => sceneLoaded == false);
//         // Entity entity = await Oasis.Controllers.Blocks.Instance.Find("coal_ore");
//         // Assert.AreEqual(2, coal_ore_index);
//     }
//
//     [UnityTest]
//     public IEnumerator BlockStateFind()
//     {
//         yield return new WaitWhile(() => sceneLoaded == false);
//         yield return Run().AsCoroutine();
// #pragma warning disable 1998
//         async Task Run()
// #pragma warning restore 1998
//         {
//             Entity dirt = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Oasis..BlockStates>().Load("dirt", null);
//             // Assert.AreEqual("blockstate-dirt", em.GetName(dirt));
//         }
//     }
//
//     [UnityTest]
//     public IEnumerator BlockStatesShouldOnlyLoadOnce()
//     {
//         yield return new WaitWhile(() => sceneLoaded == false);
//         yield return Run().AsCoroutine();
// #pragma warning disable 1998
//         async Task Run()
// #pragma warning restore 1998
//         {
//             Entity dirt = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Oasis.ECS.BlockStates>().Load("dirt", null);
//             Entity dirt2 = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Oasis.ECS.BlockStates>().Load("dirt", null);
//         }
//     }
//
//     [UnityTest]
//     public IEnumerator ChunkFind()
//     {
//         yield return new WaitWhile(() => sceneLoaded == false);
//         yield return Run().AsCoroutine();
//
// #pragma warning disable 1998
//         async Task Run()
// #pragma warning restore 1998
//         {
//             // var chunk = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Oasis.ECS.Chunks>().Load(new int3(0));
//             // Assert.AreEqual("chunk-0,0,0", em.GetName(chunk));
//         }
//     }
//
//     [UnityTest]
//     public IEnumerator ModelFind()
//     {
//         yield return new WaitWhile(() => sceneLoaded == false);
//         yield return Run().AsCoroutine();
//
// #pragma warning disable 1998
//         async Task Run()
// #pragma warning restore 1998
//         {
//             var models = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Oasis.ECS.Models>();
//             Entity iron_ore = models.Load(new DomainName(){ name = "iron_ore"});
//             Entity grass = models.Load(new DomainName() {name = "iron_ore"});
//             // Assert.AreEqual("Model dirt", em.GetName(grass));
//         }
//     }
//
//
//     [UnityTest]
//     public IEnumerator TextureFind()
//     {
//         yield return new WaitWhile(() => sceneLoaded == false);
//         yield return Run().AsCoroutine();
//
// #pragma warning disable 1998
//         async Task Run()
// #pragma warning restore 1998
//         {
//             // Entity grassEntity = await Textures.Instance.Load(new Oasis.Core.FQDN("grass", "oasis"));
//             // Entity dirtEntity = await Textures.Instance.Load(new Oasis.Core.FQDN("dirt", "oasis"));
//             // Entity coalOreEntity = await Textures.Instance.Load(new Oasis.Core.FQDN("coal_ore","oasis"));
//
//             // Assert.AreEqual("Texture grass", em.GetName(grassEntity));
//             // Assert.AreEqual("Texture dirt", em.GetName(dirtEntity));
//             // Assert.AreEqual("Texture coal_ore", em.GetName(coalOreEntity));
//         }
//     }
//     
//     [UnityTest]
//     public IEnumerator TextureFindShouldOnlyLoadOnce()
//     {
//         yield return new WaitWhile(() => sceneLoaded == false);
//         yield return Run().AsCoroutine();
//
// #pragma warning disable 1998
//         async Task Run()
// #pragma warning restore 1998
//         {
//             // Entity grass_index = await Oasis.Controllers.Textures.Instance.Load(new Oasis.Core.FQDN("grass","oasis"));
//             // await Textures.Instance.Load(new Oasis.Core.FQDN("grass", "oasis"));
//             // await Textures.Instance.Load(new Oasis.Core.FQDN("grass", "oasis"));
//             // TODO
//         }
//     }
// }
//
//
// public static class TestExtensionMethods
// {
//     public static IEnumerator AsCoroutine(this Task task)
//     {
//         while (!task.IsCompleted) yield return null;
//         // if task is faulted, throws the exception
//         task.GetAwaiter().GetResult();
//     }
// }
//
//
// // Entity CreateChunk(int3 id)
// // {
// //     //Entity dataEntity = await Load(id);
// //     Entity entity = em.CreateEntity(typeof(Chunk), typeof(LocalToWorld));
// //     Chunks.Instance.instances[id] = entity;
// //     EntityHelpers.SetName(entity, $"Chunk {id.ToStr()}");
// //     em.AddComponentData<Id3>(entity, new Id3 { Value = id });
// //     //em.AddComponentData<DataEntity>(entity, new DataEntity { Value = dataEntity });
//
// //     float3 position = new float3(id) * 16;
//
// //     em.AddComponentData<LocalToWorld>(entity, new LocalToWorld { Value = new float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0) });
// //     em.AddComponentData<Translation>(entity, new Translation { Value = position });
// //     return entity;
// // }