using UnityEngine;
using System.Collections;

public class cloth_motion: MonoBehaviour {

	float 		t;
	int[] 		edge_list;
	float 		mass;
	float		damping;
	float 		stiffness;
	float[] 	L0;
	Vector3[] 	velocities;


	// Use this for initialization
	void Start () 
	{
		t 			= 0.075f;
		mass 		= 1.0f;
		damping 	= 0.99f;
		stiffness 	= 1000.0f;

		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		int[] 		triangles = mesh.triangles;
		Vector3[] 	vertices = mesh.vertices;

        //Construct the original edge list
        int[] original_edge_list = new int[triangles.Length*2];
		for (int i=0; i<triangles.Length; i+=3) 
		{
			original_edge_list[i*2+0]=triangles[i+0];
			original_edge_list[i*2+1]=triangles[i+1];
			original_edge_list[i*2+2]=triangles[i+1];
			original_edge_list[i*2+3]=triangles[i+2];
			original_edge_list[i*2+4]=triangles[i+2];
			original_edge_list[i*2+5]=triangles[i+0];
		}
		//Reorder the original edge list
		for (int i=0; i<original_edge_list.Length; i+=2)
			if(original_edge_list[i] > original_edge_list[i + 1]) 
				Swap(ref original_edge_list[i], ref original_edge_list[i+1]);
		//Sort the original edge list using quicksort
		Quick_Sort (ref original_edge_list, 0, original_edge_list.Length/2-1);

		int count = 0;
		for (int i=0; i<original_edge_list.Length; i+=2)
			if (i == 0 || 
			    original_edge_list [i + 0] != original_edge_list [i - 2] ||
			    original_edge_list [i + 1] != original_edge_list [i - 1]) 
					count++;

		edge_list = new int[count * 2];
		int r_count = 0;
		for (int i=0; i<original_edge_list.Length; i+=2)
			if (i == 0 || 
			    original_edge_list [i + 0] != original_edge_list [i - 2] ||
				original_edge_list [i + 1] != original_edge_list [i - 1]) 
			{
				edge_list[r_count*2+0]=original_edge_list [i + 0];
				edge_list[r_count*2+1]=original_edge_list [i + 1];
				r_count++;
			}


		L0 = new float[edge_list.Length/2];
		for (int e=0; e<edge_list.Length/2; e++) 
		{
			int v0 = edge_list[e*2+0];
			int v1 = edge_list[e*2+1];
			L0[e]=(vertices[v0]-vertices[v1]).magnitude;
		}

		velocities = new Vector3[vertices.Length];
		for (int v=0; v<vertices.Length; v++)
			velocities [v] = new Vector3 (0, 0, 0);

		//for(int i=0; i<edge_list.Length/2; i++)
		//	Debug.Log ("number"+i+" is" + edge_list [i*2] + "and"+ edge_list [i*2+1]);
	}

	void Quick_Sort(ref int[] a, int l, int r)
	{
		int j;
		if(l<r)
		{
			j=Quick_Sort_Partition(ref a, l, r);
			Quick_Sort (ref a, l, j-1);
			Quick_Sort (ref a, j+1, r);
		}
	}

	int  Quick_Sort_Partition(ref int[] a, int l, int r)
	{
		int pivot_0, pivot_1, i, j;
		pivot_0 = a [l * 2 + 0];
		pivot_1 = a [l * 2 + 1];
		i = l;
		j = r + 1;
		while (true) 
		{
			do ++i; while( i<=r && (a[i*2]<pivot_0 || a[i*2]==pivot_0 && a[i*2+1]<=pivot_1));
			do --j; while(  a[j*2]>pivot_0 || a[j*2]==pivot_0 && a[j*2+1]> pivot_1);
			if(i>=j)	break;
			Swap(ref a[i*2], ref a[j*2]);
			Swap(ref a[i*2+1], ref a[j*2+1]);
		}
		Swap (ref a [l * 2 + 0], ref a [j * 2 + 0]);
		Swap (ref a [l * 2 + 1], ref a [j * 2 + 1]);
		return j;
	}

	void Swap(ref int a, ref int b)
	{
		int temp = a;
		a = b;
		b = temp;
	}

    /*
    void Strain_Limiting(Vector3[] vertices)
    {
        float w = .2f;
        Vector3 xiSum = Vector3.zero;
        for (int j = 0; j < vertices.Length; j++) { 

            xiSum = Vector3.zero;
            for (int i = 0; i < edge_list.Length; i = i + 2)
            {
                if (vertices[edge_list[i]] == vertices[j] && i < L0.Length)
                {
                    float le0 = L0[i];
                    Vector3 xi = vertices[edge_list[i]];
                    Vector3 xj = vertices[edge_list[i + 1]];
                    Vector3 xei = (1 / 2) * (xi + xj + le0 * ((xi - xj) / ((xi - xj).magnitude)));
                    xiSum += xei;
                }
            }
            for (int i = 1; i < edge_list.Length; i = i + 2)
            {
                if (vertices[edge_list[i]] == vertices[j] && i < L0.Length)
                {
                    float le0 = L0[i - 1];
                    Vector3 xi = vertices[edge_list[i]];
                    Vector3 xj = vertices[edge_list[i - 1]];
                    Vector3 xei = (1 / 2) * (xi + xj + le0 * ((xj - xi) / ((xj - xi).magnitude)));
                    xiSum += xei;
                }
            }
            Vector3 xiNew = (w * vertices[j] + xiSum) / (w + edge_list.Length / 2);
            vertices[j] = xiNew;
            velocities[j] = velocities[j] + (1 / t) * (xiNew - vertices[j]);
        }
	}*/

    void Strain_Limiting(Vector3[] vertices)
    {
        float w = .2f;
        Vector3[] sum_x = new Vector3[vertices.Length];
        for (int v = 0; v < sum_x.Length; v++)
            sum_x[v] = new Vector3(0, 0, 0);
        int[] sum_N = new int[vertices.Length];
        for (int v = 0; v < sum_N.Length; v++)
            sum_N[v] = 0;

        //Edge Loop
        for (int e = 0; e < L0.Length; e++)
        {
            int i = 2 * e;
            int j = 2 * e + 1;
            Vector3 xi = vertices[edge_list[i]];
            Vector3 xj = vertices[edge_list[j]];
            Vector3 xei = (1 / 2) * (xi + xj + L0[e] * ((xi - xj) / ((xi - xj).magnitude)));
            Vector3 xej = (1 / 2) * (xi + xj + L0[e] * ((xj - xi) / ((xi - xj).magnitude)));
            sum_x[edge_list[i]] += xei;
            sum_x[edge_list[j]] += xej;
            sum_N[edge_list[i]]++;
            sum_N[edge_list[j]]++;
        }
        

        //Vertex Loop
        for (int i = 0; i < vertices.Length; i++)
        {
            if (i != 0 && i != 10)
            {
                Vector3 xNew = ((w * vertices[i] + sum_x[i]) / (w + sum_N[i]));
                velocities[i] += (1 / t) * (xNew - vertices[i]);
                vertices[i] = xNew;

            }
        }







        /*
        for (int i = 0; i < vertices.Length; i++)
        {
            if (i != 0 && i != 10)
            {
                Vector3 xiSum = Vector3.zero;
                int k = 0;
                xiNew[i] = vertices[i];
                for (int j = 0; j < edge_list.Length; j++)
                {
                    if (vertices[i] == vertices[edge_list[j]])
                    {
                        Vector3 xi = Vector3.zero;
                        Vector3 xj = Vector3.zero;
                        Vector3 xei = Vector3.zero;
                        if (j % 2 == 0 || j == 0)
                        {
                            xi = vertices[edge_list[j]];
                            xj = vertices[edge_list[j + 1]];
                            xei = (1 / 2) * (xi + xj + L0[j / 2] * ((xi - xj) / ((xi - xj).magnitude)));
                        } else
                        {
                            xi = vertices[edge_list[j - 1]];
                            xj = vertices[edge_list[j]];
                            xei = (1 / 2) * (xi + xj + L0[j / 2] * ((xi - xj) / ((xi - xj).magnitude)));
                        }
                        xiSum += xei;
                        k++;
                    }
                }
                xiNew[i] = ((w * vertices[i] + xiSum) / (w + k));
                velocities[i] = velocities[i] + (1 / t) * (xiNew[i] - vertices[i]);

                vertices[i] = xiNew[i];
            }
        }*/
    }


    void Collision_Handling()
	{

	}

	// Update is called once per frame
	void Update () 
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            if (i != 0 && i != 10)
            {
                velocities[i] += new Vector3(0, -9.81f, 0) * t;
                velocities[i] *= damping;
                vertices[i] += t * velocities[i];
            }
        }

        for (int h = 0; h < 64; h++)
        {
            Strain_Limiting(vertices);
        }
        //vertices = xiNew;
        /*
        for (int i = 0; i < vertices.Length; i++)
        {
            velocities[i] *= damping;
            if (i != 0 && i != 10)
            {
                vertices[i] += (velocities[i] * t);

            }
        }*/


        mesh.vertices = vertices;
		mesh.RecalculateNormals();

	}

}
