using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct BoltLineAnchor
{
    public Vector3 StartPoint;
    public Vector3[] SectionPoint;
    public Vector3 EndPoint;
    public float Lifetime;
    public float Duration;

    public BoltLineAnchor(Vector3 startPoint, Vector3[] sectionPoint, Vector3 endPoint, float lifetime, float duration)
    {
        StartPoint = startPoint;
        SectionPoint = sectionPoint;
        EndPoint = endPoint;
        Lifetime = lifetime;
        Duration = duration;
    }
}
[Serializable]
public struct BoltLine
{
    public List<Vector3> Up;
    public List<Vector3> Down;
    public float Lifetime;
    public float Duration;
}

public struct MinMaxFloat
{
    public float Min;
    public float Max;
}

public struct MinMaxInt
{
    public int Min;
    public int Max;
}


[RequireComponent(typeof(CapsuleCollider))]
public class BoltFx : MonoBehaviour
{
//    [Range(0,1)]public float RandomMap;
    public Vector2 Duration = new Vector2(0.2f, 0.4f);   //闪电持续时间
    public int MaxNumber = 10;    //最大闪电数
    public Vector2 RefreshTime = new Vector2( 0.5f, 1f);    //闪电刷新时间
    public Gradient ColorOverLifeTime;
    public AnimationCurve SizeOverLifeTime;

    [Space]
    public Vector2Int RefreshNumber = new Vector2Int(2, 5);    //单次刷新闪电数
    public float OffsetRadius = 0.5f;    //闪电偏移半径
    public float ShakeFrequency = 0.2f;    //抖动刷新时间
    public float Amplitude = 2;    //抖动幅度(振幅)
    [Range(0, 0.5f)] public float ArcOffset = 0.25f;
    public float MeshWidth = 2;    //闪电宽度
    public Vector2Int MeshSegment = new Vector2Int(4, 8);    //闪电分段数
    public Material Material;
    private List<BoltLineAnchor> _boltLineAnchors;
    private List<BoltLine> _meshPoint;
    private List<Mesh> _mesh;
    private Mesh _totalMesh;
//    private Vector3 _scale;
    private Vector3 _viewDir;
    private Camera _view;
    private CapsuleCollider _collider;

    private List<Vector3> TotalVert;
    private List<Vector2> TotalUV;
    private List<Color> TotalColor;
    private List<int> TotalTriangle;

    private float _realheight;
    private float _boltRefreshTimer;
    private float _shakeRefreshTimer;
    private int _realRefreshNumber;
    private float _realRefreshTime;
    private static Vector3 linepoint = Vector3.up;
    private void Awake()
    {
//        _scale = transform.localScale;
//        _view = Camera.main;
        _mesh = new List<Mesh>();
        _boltLineAnchors = new List<BoltLineAnchor>();
        _meshPoint = new List<BoltLine>();
        _collider = GetComponent<CapsuleCollider>();

        _totalMesh = new Mesh();
        TotalColor = new List<Color>();
        TotalTriangle = new List<int>();
        TotalVert = new List<Vector3>();
        TotalUV = new List<Vector2>();
        _view = Camera.main;
    }

    private void Start()
    { 
        _realRefreshNumber = Random.Range(RefreshNumber.x, RefreshNumber.y);
        for (int i = 0; i < _realRefreshNumber; i++)
        {
            _boltLineAnchors.Add(CreateAnchor());
        }

        _realheight = _collider.height - 2 * _collider.radius;
        _realRefreshTime = Random.Range(RefreshTime.x, RefreshTime.y);
    }

    private void OnValidate()
    {
        Duration.x = Duration.x < 0 ? 0 : Duration.x;
        Duration.y = Duration.y < Duration.x ? Duration.x : Duration.y;
        MaxNumber = MaxNumber < 0 ? 0 : MaxNumber;
        RefreshTime.x = RefreshTime.x < 0 ? 0 : RefreshTime.x;
        RefreshTime.y = RefreshTime.y < RefreshTime.x ? RefreshTime.x : RefreshTime.y;
        RefreshNumber.x = RefreshNumber.x < 1 ? 1 : (RefreshNumber.x > MaxNumber ? MaxNumber : RefreshNumber.x);
        RefreshNumber.y = RefreshNumber.y > MaxNumber ? MaxNumber : (RefreshNumber.y < RefreshNumber.x ? RefreshNumber.x : RefreshNumber.y);
        OffsetRadius = OffsetRadius < 0 ? 0 : OffsetRadius;
        ShakeFrequency = ShakeFrequency < 0 ? 0 : ShakeFrequency;
        Amplitude = Amplitude < 0 ? 0 : Amplitude;
        MeshWidth = MeshWidth < 0 ? 0 : MeshWidth;
        MeshSegment.x = MeshSegment.x < 3 ? 3 : MeshSegment.x;
        MeshSegment.y = MeshSegment.y < MeshSegment.x ? MeshSegment.x : MeshSegment.y;
    }

    
    private void Update()
    {

        _meshPoint.Clear();
        //加入新的闪电
        if (_boltRefreshTimer > _realRefreshTime)
        {
            _realRefreshNumber = Random.Range(RefreshNumber.x, RefreshNumber.y);
            for (int i = 0; i < _realRefreshNumber; i++)
            {
                _boltLineAnchors.Add(CreateAnchor());
            }
            _boltRefreshTimer = 0;
            _realRefreshTime = Random.Range(RefreshTime.x, RefreshTime.y);
        }

        //刷新时间并移除到期闪电
        for (int i = 0; i < _boltLineAnchors.Count; i++)
        {
            var boltLineAnchor = _boltLineAnchors[i];
            boltLineAnchor.Lifetime = _boltLineAnchors[i].Lifetime + Time.deltaTime;
            _boltLineAnchors[i] = boltLineAnchor;
            if (_boltLineAnchors[i].Lifetime >= _boltLineAnchors[i].Duration)
            {
                _boltLineAnchors.Remove(_boltLineAnchors[i]);
            }
        }

        //移除多余闪电节点
        if (_boltLineAnchors.Count > MaxNumber)
        {
            _boltLineAnchors.RemoveRange(0,_boltLineAnchors.Count - MaxNumber);
        }  

        //抖动并生成面片顶点坐标
        if (_shakeRefreshTimer > ShakeFrequency)
        {
            for (var index = 0; index < _boltLineAnchors.Count; index++)
            {
                CreateSubPoint(_boltLineAnchors[index]);
//                _meshPoint[index] = CreateMeshPoint(_boltLineAnchors[index]);
               
                _meshPoint.Add(CreateMeshPoint(_boltLineAnchors[index]));
            }

            _shakeRefreshTimer = 0;
        }
        else
        {
            for (var index = 0; index < _boltLineAnchors.Count; index++)
            {
//                _meshPoint[index] = CreateMeshPoint(_boltLineAnchors[index]);
               _meshPoint.Add(CreateMeshPoint(_boltLineAnchors[index]));
            }
        }
        
        //清理Mesh
        foreach (Mesh bolt in _mesh)
        {
            bolt.Clear();
            Destroy(bolt);
        }
        
        //清空Mesh列表
        _mesh.Clear();
        _totalMesh.Clear();
//        Destroy(_totalMesh);
//        _totalMesh = new Mesh();
        TotalColor.Clear();
        TotalTriangle.Clear();
        TotalVert.Clear();
        TotalUV.Clear();
        //生成Mesh数据
        for (var index = 0; index < _meshPoint.Count; index++)
        {
            BoltLine boltLine = _meshPoint[index];
//            _mesh.Add(CreateMesh(boltLine));
            CreateMesh(boltLine);
        }

        //绘制Mesh
//        foreach (Mesh bolt in _mesh)
//        {
//            Graphics.DrawMesh(bolt, transform.localToWorldMatrix, Material, 0);
//        }
        _totalMesh.vertices = TotalVert.ToArray();
        _totalMesh.uv = TotalUV.ToArray();
        _totalMesh.triangles = TotalTriangle.ToArray();
        _totalMesh.colors = TotalColor.ToArray();
        Graphics.DrawMesh(_totalMesh,transform.position,Quaternion.identity,Material,0);
        
        _boltRefreshTimer += Time.deltaTime;
        _shakeRefreshTimer += Time.deltaTime;
    }

    private void OnDrawGizmos()
    {
//        Gizmos.DrawLine(transform.position + transform.localToWorldMatrix, transform.position - _realheight * 0.5f);
        
        if (_boltLineAnchors != null)
        {
            foreach (BoltLineAnchor bla in _boltLineAnchors)
            {
                for (int i = 1; i < bla.SectionPoint.Length; i++)
                {
                    Gizmos.color = new Color(1,1,1);
                    Gizmos.DrawLine(bla.SectionPoint[i] + transform.position,bla.SectionPoint[i-1] + transform.position);
                }
                Gizmos.DrawIcon(bla.StartPoint + transform.position, "CollabMoved.tiff");
                Gizmos.DrawIcon(bla.EndPoint + transform.position, "CollabMoved.tiff");
                Gizmos.color = Color.green;
                Gizmos.DrawLine(bla.StartPoint + transform.position,bla.SectionPoint[0] + transform.position);
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(bla.EndPoint + transform.position,bla.SectionPoint[bla.SectionPoint.Length-1] + transform.position);
            }
        }
        if (_meshPoint != null)
        {
            foreach (BoltLine bl in _meshPoint)
            {
                for (int i = 1; i < bl.Up.Count; i++)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(bl.Up[i] + transform.position,bl.Up[i-1] + transform.position);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(bl.Down[i] + transform.position,bl.Down[i-1] + transform.position);
                }
                
            }
        }
    }

    private BoltLineAnchor CreateAnchor()
    {
//        BoltLineAnchor anchor = new BoltLineAnchor
//        {
//            StartPoint = RandomCreateStartPoint()
//        };
//        //        anchor.EndPoint = RandomCreatePoint(0.5f,_scale);
//        anchor.EndPoint = RandomCreatePoint(OffsetRadius, anchor.StartPoint);
        BoltLineAnchor anchor = testRandom();
        int realSegment = Random.Range(MeshSegment.x, MeshSegment.y);
        anchor.Lifetime = 0;
        anchor.Duration = Random.Range(Duration.x, Duration.y);
        anchor.SectionPoint = new Vector3[realSegment -1];
        if (realSegment > 1)
        {
            CreateSubPoint(anchor);
        }
        
        return anchor;
    }
//    private Vector3 RandomCreateStartPoint()
//    {
//        float radius = _collider.height + _collider.radius;
//        Vector3 standard = new Vector3(Random.Range(-radius,radius),Random.Range(-radius,radius),Random.Range(-radius,radius));
//        standard = _collider.ClosestPoint(standard + transform.position) - transform.position;
//        return standard;
//        
//    }
//    private Vector3 RandomCreatePoint(float radius, Vector3 position = default)
//    {
//        Vector3 standard;
//        standard = 0.57735f * radius * new Vector3(1, 1, 1);
//        standard = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * standard;
//        standard = _collider.ClosestPoint(standard + position + transform.position) - transform.position;
//        return standard;
//    }

    private BoltLineAnchor testRandom()
    {
       
//        float axispoint = Random.Range(-_collider.radius, _realheight + _collider.radius);
        BoltLineAnchor target = new BoltLineAnchor();
//        target.StartPoint = linepoint * axispoint + RandomPoint(axispoint) - _realheight * 0.5f * linepoint;
        float axispoint = Random.Range(0, _realheight + 2 * _collider.radius);
        target.StartPoint = RandomPoint2(axispoint,_collider.radius);
        target.StartPoint = transform.localToWorldMatrix * target.StartPoint;
//        target.StartPoint = transform.TransformVector(target.StartPoint);
//        float endaxispoint = Random.Range(axispoint + OffsetRadius, axispoint - OffsetRadius);
//        target.EndPoint = linepoint * endaxispoint + RandomPoint(endaxispoint)- 0.5f * _realheight * linepoint;
        axispoint = Mathf.Clamp(Random.Range(-OffsetRadius,OffsetRadius) + axispoint,0, _realheight + 2 * _collider.radius);
//        target.EndPoint = TotalRandom(OffsetRadius) + target.StartPoint;
        target.EndPoint = RandomPoint2(axispoint,_collider.radius + OffsetRadius);
        target.EndPoint = transform.localToWorldMatrix * target.EndPoint;
//        target.EndPoint = transform.TransformVector(target.EndPoint);
        return target;
    }

    private Vector3 RandomPoint2(float axispoint, float radius)
    {
        Vector3 standard = Vector3.forward;
//        float axispoint = Random.Range(0, _realheight + 2 * _collider.radius);
        if (axispoint < _collider.radius * 2)
        {
            standard = Random.rotation * standard * radius;
            if (standard.y < 0)
            {
                standard -= _realheight * 0.5f * Vector3.up;
            }
            else
            {
                standard += _realheight * 0.5f * Vector3.up;
            }
        }
        else
        {
            float random = Random.Range(0, 2 * Mathf.PI);
            standard = new Vector3(Mathf.Sin(random), 0, Mathf.Cos(random)) * _collider.radius + linepoint * (axispoint - radius * 2 - _realheight * 0.5f);
        }
        return standard;
    }

    private Vector3 TotalRandom(float radius)
    {
        Vector3 standard = Vector3.forward;
        standard = Random.rotation * standard * radius;
        return standard;
    }
    private Vector3 RandomPoint(float axispoint)
    {
        
        Vector3 standard = Vector3.one;
        if (axispoint > 0 && axispoint < _realheight)
        { 
            standard = Vector3.right * _collider.radius;
            Debug.Log(standard+"MID: "+Vector3.right +" * "+ _collider.radius);

        }else if (axispoint < 0)
        {

            if (axispoint < -_collider.radius)
            {
                axispoint = -_collider.radius;
            }
            float angle = Mathf.Acos(-axispoint / _collider.radius);
            standard = -axispoint * Mathf.Tan(angle / 6.2831f) * _collider.radius * Vector3.right;
            Debug.Log(standard+"负： "+(-axispoint) +" * "+Mathf.Tan(angle / 6.2831f)+" * "+ _collider.radius +" * "+Vector3.right);
        }
        else
        {
            
            if (axispoint > _collider.radius + _realheight)
            {
                axispoint = _collider.radius + _realheight;
            }
            float angle = Mathf.Acos((axispoint - _realheight) / _collider.radius);
            standard = (axispoint - _realheight) * Mathf.Tan(angle / 6.2831f) * _collider.radius * Vector3.right;
            Debug.Log( standard+"正： "+"("+axispoint+" - "+_realheight+") * "+Mathf.Tan(angle / 6.2831f)+" * "+ _collider.radius +" * "+Vector3.right);
        }
        return Quaternion.AngleAxis(Random.Range(0, 360), linepoint) * standard;
    }
    

    private void CreateSubPoint(BoltLineAnchor uninitAnchor)
    {
        Vector3 dir = uninitAnchor.EndPoint - uninitAnchor.StartPoint;
        Vector3 subDir = dir / (uninitAnchor.SectionPoint.Length + 1);
        Vector3 randomDir = new Vector3();
        float mid = (uninitAnchor.SectionPoint.Length + 1) * 0.5f;
        float sideP;
        
        randomDir = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * new Vector3(1, 1, 1) * Amplitude * 0.01f * (1 - uninitAnchor.Lifetime / uninitAnchor.Duration);
        uninitAnchor.StartPoint = uninitAnchor.StartPoint + randomDir;
//        uninitAnchor.SectionPoint[0] = (uninitAnchor.StartPoint + subDir)*(1 + (-Mathf.Abs(1/ mid - 1) + 1) * ArcOffset) + randomDir;
        uninitAnchor.SectionPoint[0] = uninitAnchor.StartPoint + subDir;
        uninitAnchor.SectionPoint[0] = uninitAnchor.SectionPoint[0] +
                                       (-Mathf.Abs(1 / mid - 1) + 1) * ArcOffset * uninitAnchor.SectionPoint[0].normalized + randomDir;
        for (int i = 1; i < uninitAnchor.SectionPoint.Length; i++)
        {
            sideP = -Mathf.Abs((i + 1) / mid - 1) + 1;
            randomDir = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * new Vector3(1, 1, 1) * Amplitude * 0.01f * (1 - uninitAnchor.Lifetime / uninitAnchor.Duration);
//            uninitAnchor.SectionPoint[i] = (uninitAnchor.StartPoint + subDir * (i+1)) * (1 + sideP * ArcOffset) + randomDir;
            uninitAnchor.SectionPoint[i] = uninitAnchor.StartPoint + subDir * (i + 1);
            uninitAnchor.SectionPoint[i] = uninitAnchor.SectionPoint[i] + sideP * ArcOffset * uninitAnchor.SectionPoint[i].normalized + randomDir;

        }
        randomDir = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * new Vector3(1, 1, 1) * Amplitude * 0.01f * (1 - uninitAnchor.Lifetime /uninitAnchor.Duration);
        uninitAnchor.EndPoint = uninitAnchor.SectionPoint[uninitAnchor.SectionPoint.Length - 1] + randomDir;
    }
    
    private BoltLine CreateMeshPoint(BoltLineAnchor boltLineAnchor)
    {
        BoltLine newBoltline = new BoltLine
        {
            Up = new List<Vector3>(), Down = new List<Vector3>(), Lifetime = boltLineAnchor.Lifetime, Duration = boltLineAnchor.Duration
        };
       Vector3 next = new Vector3();
       Vector3 old = new Vector3();
       float sideP = 0;
       //first
       next = (boltLineAnchor.SectionPoint.Length > 0 ? boltLineAnchor.SectionPoint[0] : boltLineAnchor.EndPoint) - boltLineAnchor.StartPoint;
       Vector3 normal = Vector3.Cross(_viewDir, next);
       newBoltline.Up.Add(boltLineAnchor.StartPoint);
       newBoltline.Down.Add(boltLineAnchor.StartPoint);

       float mid = (boltLineAnchor.SectionPoint.Length + 1) * 0.5f;
       //sub
       for (int i = 0; i < boltLineAnchor.SectionPoint.Length-1; i++)
       {
           sideP = - Mathf.Abs((i + 1) / mid -1) +1;
           old = next;
           next = boltLineAnchor.SectionPoint[i + 1] - boltLineAnchor.SectionPoint[i];
           normal = Vector3.Cross(old, next);
           newBoltline.Up.Add(boltLineAnchor.SectionPoint[i] + MeshWidth * 0.01f * sideP * SizeOverLifeTime.Evaluate(boltLineAnchor.Lifetime / boltLineAnchor.Duration) * normal.normalized);
           newBoltline.Down.Add(boltLineAnchor.SectionPoint[i] + MeshWidth * 0.01f * -1 * sideP * SizeOverLifeTime.Evaluate(boltLineAnchor.Lifetime / boltLineAnchor.Duration)  * normal.normalized);  
       }
       sideP = - Mathf.Abs((boltLineAnchor.SectionPoint.Length) / mid -1)  +1;
       old = next;
       next = boltLineAnchor.EndPoint - (boltLineAnchor.SectionPoint.Length > 0 ? boltLineAnchor.SectionPoint[boltLineAnchor.SectionPoint.Length - 1] : boltLineAnchor.StartPoint);
       normal = Vector3.Cross(old, next);
       newBoltline.Up.Add(boltLineAnchor.SectionPoint[boltLineAnchor.SectionPoint.Length - 1] + MeshWidth * 0.01f * sideP * SizeOverLifeTime.Evaluate(boltLineAnchor.Lifetime / boltLineAnchor.Duration)  * normal.normalized);
       newBoltline.Down.Add(boltLineAnchor.SectionPoint[boltLineAnchor.SectionPoint.Length - 1] + MeshWidth * 0.01f * -1 * sideP * SizeOverLifeTime.Evaluate(boltLineAnchor.Lifetime / boltLineAnchor.Duration)  * normal.normalized); 
       
       //End
       newBoltline.Up.Add(boltLineAnchor.EndPoint);
       newBoltline.Down.Add(boltLineAnchor.EndPoint);
       
       
       return newBoltline;
    }

    private void CreateMesh(BoltLine boltline)
    {
        List<Vector3> vertList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();
        List<int> tri = new List<int>();
        List<Color> vertColors = new List<Color>();
        
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
            int ul = i * 2 + TotalVert.Count;
            tri.Add(ul);
            tri.Add(ul + 3);
            tri.Add(ul + 1);
            tri.Add(ul);
            tri.Add(ul + 2);
            tri.Add(ul + 3);
        }
        for (int i = 0; i < vertList.Count; i++)
        {
            vertColors.Add(ColorOverLifeTime.Evaluate(boltline.Lifetime / boltline.Duration));
        }

        TotalVert.AddRange(vertList);
        TotalVert = TotalVert.Concat(vertList).ToList();
        TotalUV.AddRange(uvList);
        TotalUV = TotalUV.Concat(uvList).ToList();
        TotalTriangle.AddRange(tri);
        TotalTriangle = TotalTriangle.Concat(tri).ToList();
        TotalColor.AddRange(vertColors);
        TotalColor = TotalColor.Concat(vertColors).ToList();
        
    }
}
