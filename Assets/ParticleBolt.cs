using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ParticleBolt : MonoBehaviour
{
    public float OffsetRadius = 0.5f;    //闪电偏移半径
    public int RefreshNumber = 5;    //单次刷新闪电数
    public float ShakeTime = 0.2f;    //抖动刷新时间
    public float ShakeLevel = 2;    //抖动幅度
    public float MeshWidth = 2;    //闪电宽度
    public int MeshSegment = 5;
    public Material Material;
    private List<BoltLineAnchor> _boltLineAnchors;
    private List<BoltLine> _meshPoint;
    private List<Mesh> _mesh;
    private Vector3 _scale;
    private Vector3 _viewDir;
    private Camera _view;
    private ParticleSystem _particleSystem;
    private ParticleSystemRenderer _particleSystemRenderer;
    
    
    private BoltLineAnchor testA;
    private float _boltRefreshTimer;
    private float _shakeRefreshTimer;
    private float _destoryTimer;
    
    private void Awake()
    {
        _view = Camera.main;
        _particleSystem = GetComponent<ParticleSystem>();
        _particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
        _mesh = new List<Mesh>();
        _boltLineAnchors = new List<BoltLineAnchor>();
        _meshPoint = new List<BoltLine>();
    }

    private void Update()
    {
        _meshPoint.Clear();
        if (_boltRefreshTimer > _particleSystem.main.duration)
        {
            for (int i = 0; i < RefreshNumber; i++)
            {
                testA = CreateAnchor();
                _boltLineAnchors.Add(testA);
                if (_boltLineAnchors.Count > _particleSystem.main.maxParticles)
                {
                    _boltLineAnchors.RemoveAt(0);
                }
            }
            
            _boltRefreshTimer = 0;
        }

//        if (_destoryTimer > _particleSystem.main.duration)
//        {
//            for (int i = 0; i < RefreshNumber; i++)
//            {
//                if (_boltLineAnchors.Count > 0)
//                {
//                    _boltLineAnchors.RemoveAt(0);
//                }  
//            }
//            _destoryTimer = 0;
//        }
//        

        if (_shakeRefreshTimer > ShakeTime)
        {
            foreach (BoltLineAnchor anchor in _boltLineAnchors)
            {
                RefreshSubPoint(anchor);
                _meshPoint.Add(CreateMeshPoint(anchor));
            }
            _shakeRefreshTimer = 0;
        }
        else
        {
            foreach (BoltLineAnchor anchor in _boltLineAnchors)
            {
                _meshPoint.Add(CreateMeshPoint(anchor));
            }
        }

        foreach (Mesh bolt in _mesh)
        {
            bolt.Clear();
        }
        
        _mesh.Clear();
        
        foreach (BoltLine boltLine in _meshPoint)
        {
            _mesh.Add(CreateMesh(boltLine));;
        }

        Mesh[] meshes = _mesh.ToArray();
//        Debug.Log(meshes.Length);
        _particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        _particleSystemRenderer.SetMeshes(meshes,5);
//        foreach (Mesh bolt in _mesh)
//        {
//            Graphics.DrawMesh(bolt, transform.localToWorldMatrix, Material, 0);
//        }
        
        _boltRefreshTimer += Time.deltaTime;
        _shakeRefreshTimer += Time.deltaTime;
        _destoryTimer += Time.deltaTime;
        
    }

    // Start is called before the first frame update
    private BoltLineAnchor CreateAnchor()
    {
        BoltLineAnchor anchor = new BoltLineAnchor();
        anchor.SectionPoint = new Vector3[MeshSegment];
        anchor.StartPoint = Vector3.zero;
//        anchor.EndPoint = RandomCreatePoint(0.5f,_scale);
        anchor.EndPoint = RandomCreatePoint(OffsetRadius);
        anchor.SectionPoint = new Vector3[MeshSegment -1];
        RefreshSubPoint(anchor);
        return anchor;
    }
    private Vector3 RandomCreatePoint(float radius)
    {
        Vector3 standard = 0.57735f * radius * new Vector3(1, 1, 1);
        standard = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * standard;
        return standard;
    }

    private BoltLineAnchor RefreshSubPoint(BoltLineAnchor uninitAnchor)
    {
        Vector3 dir = uninitAnchor.EndPoint - uninitAnchor.StartPoint;
        Vector3 subDir = dir / (MeshSegment - 1);
        Vector3 randomDir = new Vector3();
        randomDir = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * new Vector3(1, 1, 1) * ShakeLevel * 0.01f;
        uninitAnchor.StartPoint += randomDir;
        randomDir = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * new Vector3(1, 1, 1) * ShakeLevel * 0.01f;
        uninitAnchor.EndPoint += randomDir;
        for (int i = 1; i < MeshSegment; i++)
        {
            randomDir = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * new Vector3(1, 1, 1) * ShakeLevel * 0.01f;
            uninitAnchor.SectionPoint[i - 1] = (uninitAnchor.StartPoint + i * subDir).normalized + randomDir;
        }
        return uninitAnchor;
    }


    
    private BoltLine CreateMeshPoint(BoltLineAnchor boltLineAnchor)
    {
       BoltLine newBoltline = new BoltLine();
       newBoltline.Up = new List<Vector3>();
       newBoltline.Down = new List<Vector3>();
       Vector3 next = new Vector3();
       //first
       _viewDir = boltLineAnchor.StartPoint - _view.transform.position;
       next = (boltLineAnchor.SectionPoint.Length > 0 ? boltLineAnchor.SectionPoint[0] : boltLineAnchor.EndPoint) - boltLineAnchor.StartPoint;
//       Vector3 normal = new Vector3(1, 1, 1);
       Vector3 normal = Vector3.Cross(_viewDir, next);
       newBoltline.Up.Add(boltLineAnchor.StartPoint + normal.normalized * MeshWidth * 0.01f);
       newBoltline.Down.Add(boltLineAnchor.StartPoint + MeshWidth * 0.01f * -1 * normal.normalized);
       
       //sub
       for (int i = 0; i < boltLineAnchor.SectionPoint.Length-1; i++)
       {
           _viewDir = next;
           next = boltLineAnchor.SectionPoint[i + 1] - boltLineAnchor.SectionPoint[i];
           normal = Vector3.Cross(_viewDir, next);
           newBoltline.Up.Add(boltLineAnchor.SectionPoint[i] + MeshWidth * 0.01f * normal.normalized);
           newBoltline.Down.Add(boltLineAnchor.SectionPoint[i] + MeshWidth * 0.01f * -1 * normal.normalized);  
       }
       _viewDir = next;
       next = boltLineAnchor.EndPoint - (boltLineAnchor.SectionPoint.Length > 0 ? boltLineAnchor.SectionPoint[boltLineAnchor.SectionPoint.Length - 1] : boltLineAnchor.StartPoint);
       normal = Vector3.Cross(_viewDir, next);
       newBoltline.Up.Add(boltLineAnchor.SectionPoint[boltLineAnchor.SectionPoint.Length - 1] + MeshWidth * 0.01f * normal.normalized);
       newBoltline.Down.Add(boltLineAnchor.SectionPoint[boltLineAnchor.SectionPoint.Length - 1] + MeshWidth * 0.01f * -1 * normal.normalized); 
       
       //End
       _viewDir = boltLineAnchor.EndPoint - _view.transform.position;
       normal = Vector3.Cross(_viewDir, next);
       newBoltline.Up.Add(boltLineAnchor.EndPoint + MeshWidth * 0.01f * normal.normalized);
       newBoltline.Down.Add(boltLineAnchor.EndPoint + MeshWidth * 0.01f * -1 * normal.normalized);
       
       return newBoltline;
    }

    private Mesh CreateMesh(BoltLine boltline)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();
        List<int> tri = new List<int>();

        for (int i = 0; i < boltline.Up.Count; i++)
        {
            float p = 1 - i / (float)(boltline.Up.Count - 1);
            p = Mathf.Max(0,p);
            p = Mathf.Min(1,p);
            vertList.Add(boltline.Up[i]);
            vertList.Add(boltline.Down[i]);
            uvList.Add(new Vector2(p, 1));
            uvList.Add(new Vector2(p, 0));
            
        }
        for (int i = 0; i < boltline.Up.Count - 1; i++)
        {
            int ul = i * 2 + mesh.triangles.Length;
            tri.Add(ul);
            tri.Add(ul + 3);
            tri.Add(ul + 1);
            tri.Add(ul);
            tri.Add(ul + 2);
            tri.Add(ul + 3);
        }

//        mesh.vertices = vertList.ToArray();
//        mesh.uv = uvList.ToArray();
//        mesh.triangles = tri.ToArray();
//        
//        Vector3[] newVertList = new Vector3[mesh.vertices.Length + vertList.Count];
//        mesh.vertices.CopyTo(newVertList,0);
//        Vector3[] vertAdd = vertList.ToArray();
//        vertAdd.CopyTo(newVertList,mesh.vertices.Length);
//        
//        int[] newTriList = new int[mesh.triangles.Length + tri.Count];
//        mesh.triangles.CopyTo(newTriList,0);
//        int[] triAdd = tri.ToArray();
//        triAdd.CopyTo(newTriList,mesh.triangles.Length);
//        
//        Vector2[] newUVList = new Vector2[mesh.uv.Length + uvList.Count];
//        mesh.uv.CopyTo(newUVList,0);
//        Vector2[] uvAdd = uvList.ToArray();
//        uvAdd.CopyTo(newUVList,mesh.uv.Length);
//        
//        mesh.vertices = newVertList;
//        mesh.triangles = newTriList;
//        mesh.uv = newUVList;
        return mesh;
    }
}
