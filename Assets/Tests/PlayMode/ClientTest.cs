// using System;
// using System.Threading.Tasks;
// using Grpc.Core;
// using NUnit.Framework;
// using Oasis.Grpc;
//
// public class ClientTest
// {
//
//     [Test]
//     public void GetBlockStateAsync()
//     {
//         Channel channel = new Channel("server.new-oasis.com", 443, new SslCredentials());
//         OasisService.OasisServiceClient client = new OasisService.OasisServiceClient(channel);
//         var domainName = new Oasis.Grpc.DomainName() {Domain = "minecraft", Name = "coal_ore"};
//         // BlockState blockState = Task.Run(
//         //     async () =>
//         //     {
//         //         return await client.GetBlockStateModelAsync(new BlockState{Block = domainName}); 
//         //         
//         //     }).Result;
//         // Assert.AreEqual("coal_ore", blockState.Model);
//         // Assert.AreEqual("coal_ore", blockState.Block);
//     }
//
//     [Test]
//     public void GetChunkAsync() 
//     {
//         Channel channel = new Channel("server.new-oasis.com", 443, new SslCredentials());
//         OasisService.OasisServiceClient client = new OasisService.OasisServiceClient(channel);
//         Chunk chunk = Task.Run(async () =>
//         {
//             return await client.GetChunkAsync(new Int3{});
//         }).Result;
//
//         // Assert.AreEqual(palette, chunk.Palette);
//         // Assert.AreEqual(null, chunk.Voxels);
//     }
//     
//     [Test]
//     public void GetModelByNameAsync() 
//     {
//         Channel channel = new Channel("server.new-oasis.com", 443, new SslCredentials());
//         OasisService.OasisServiceClient client = new OasisService.OasisServiceClient(channel);
//         Model model = 
//             Task.Run(async () => { return await client.GetModelAsync(new DomainName{Name = "dirt"}); }).Result;
//         Assert.AreEqual("dirt", model.Name);
//     }
//
//     [Test]
//     public void GetTextureByNameAsync() 
//     {
//         Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER", "native");
//         Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "NONE"); // DEBUG
//         Environment.SetEnvironmentVariable("GRPC_TRACE", ""); // api
//         GrpcEnvironment.SetLogger(new UnityDebugLogger());
//         
//         
//         Channel channel = new Channel("server.new-oasis.com", 443, new SslCredentials());
//         OasisService.OasisServiceClient client = new OasisService.OasisServiceClient(channel);
//         Texture texture = Task.Run(async () => { return await client.GetTextureAsync(new DomainName {Name = "grass"}); }).Result;
//         Assert.AreEqual("grass", texture.Name);
//     }
//     
// }