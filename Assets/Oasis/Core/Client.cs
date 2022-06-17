using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Utils;
using Oasis.Core;
using Oasis.Grpc;
using Unity.Mathematics;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Animations;

public class Client : MonoBehaviour
{
    private static Client _instance;
    private EntityManager _em;

    public static Client Instance
    {
        get { return _instance; }
    }

    public bool secure;
    public string host;
    public int port;
    public Channel channel;
        
    public Oasis.Grpc.Oasis.OasisClient client;
    public Metadata Metadata;

    public AsyncDuplexStreamingCall<FeedRequest, FeedResponse> feed;
    public IClientStreamWriter<FeedRequest> feedRequest;
    public IAsyncStreamReader<FeedResponse> feedResponse;
    
    void Awake()
    {
        _instance = this;
        Environment.SetEnvironmentVariable("GRPC_DNS_RESOLVER", "native");
        Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "NONE"); // DEBUG
        Environment.SetEnvironmentVariable("GRPC_TRACE", ""); // api
        GrpcEnvironment.SetLogger(new UnityDebugLogger());
        
        if (secure)
        {
            channel = new Channel(host, port, new SslCredentials());
        }
        else
        {
            channel = new Channel(host, port, ChannelCredentials.Insecure);
        }
        client = new Oasis.Grpc.Oasis.OasisClient(channel);
        UpdateMetaData();
        Feed();
    }

    public void UpdateMetaData()
    {
        var token =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI2NjYiLCJleHAiOjE2NzU1MzExOTl9.d0jymUvsu9tm4IWZJSpkDATAr0oA6LgaJmlpDa7T-PE";
        Metadata = new Metadata
        {
            { "Authorization", $"Bearer {token}" }
        };
    }
    
    public async void Feed()
    {
        feed = client.Feed(Metadata);
        feedRequest = feed.RequestStream;
        feedResponse = feed.ResponseStream;

        var voxelSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<VoxelSystem>();
        
        while (await feedResponse.MoveNext())
        {
            switch (feedResponse.Current.EventCase)
            {
                case FeedResponse.EventOneofCase.VoxelChange:
                   Debug.Log("Got voxelChange from server");
                   voxelSystem._queue.Add(feedResponse.Current.VoxelChange);
                   break;
                case FeedResponse.EventOneofCase.Shutdown:
                   Debug.Log("Got shutdown from server");
                   break;
            }
        }
    }

    private void OnDisable()
    {
        feedRequest.CompleteAsync(); // Graceful
    }
}

// ECS architecture
// PaletteItem is not blittable so can't nativeQueue
// Chunks have two buffers- voxelElements and blockStateEntitiesElements
// blockStateEntities have StateElements

// TODO
// From voxelChange.PaletteItem, create blockState with stateElements




