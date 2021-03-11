﻿using ElectromagneticProblem.Enviroment;
using ElectromagneticProblem.Matrix;
using ElectromagneticProblem.SLAE;
using ElectromagneticProblem.Solvers;
using MathUtility;
using System;

namespace ElectromagneticProblem.FEM
{
	public struct ProblemInfo
	{
		public Mesh Mesh;
		public SolverTypes SolverType;
	}

	class Problem
	{
		public double[] Q { get; set; } = null;
		public Mesh Mesh { get; set; } = null;
		public ISolver Solver { get; set; } = null;

		PortraitBuilder PB;
		SLAEBuilder SB;

		public Problem(ProblemInfo info)
		{
			Mesh = info.Mesh;

			PB = new PortraitBuilder(Mesh);
			SB = new SLAEBuilder(Mesh);

			switch (info.SolverType)
			{
				case SolverTypes.LOSLLT:
					{
						Solver = new LOSLLT();
						break;
					}
				case SolverTypes.LOSLU:
					{
						Solver = new LOSLU();
						break;
					}

				default:
					{
						Solver = new LOSLU();
						break;
					}
			}
		}

		public void Solve()
		{
			MatrixPortrait MP = new MatrixPortrait();
			PB.Build(MP);

			IMatrix A = new SymmetricSparseMatrix(Mesh.NodeCount, MP);
			double[] B = new double[Mesh.NodeCount];

			SB.Build(A, B);

			Q = Solver.Solve(A, B);
		}

		public double GetValueA(Point p)
		{
			var e = FindElement(p);

			if (e != null)
			{
				Point p1 = Mesh.Points[e[0]];
				Point p2 = Mesh.Points[e[1]];
				Point p3 = Mesh.Points[e[2]];

				double x1 = p1.X;
				double x2 = p2.X;
				double y1 = p1.Y;
				double y2 = p3.Y;

				double hx = Math.Abs(p2.X - p1.X);
				double hy = Math.Abs(p3.Y - p1.Y);

				Func<Point, double> psi1 = (Point p) => (x2 - p.X) * (y2 - p.Y) / (hx * hy);
				Func<Point, double> psi2 = (Point p) => (p.X - x1) * (y2 - p.Y) / (hx * hy);
				Func<Point, double> psi3 = (Point p) => (x2 - p.X) * (p.Y - y1) / (hx * hy);
				Func<Point, double> psi4 = (Point p) => (p.X - x1) * (p.Y - y1) / (hx * hy);

				double ps1 = psi1(p);
				double ps2 = psi2(p);
				double ps3 = psi3(p);
				double ps4 = psi4(p);

				double q1 = Q[e[0]];
				double q2 = Q[e[1]];
				double q3 = Q[e[2]];
				double q4 = Q[e[3]];

				return psi1(p) * q1 + psi2(p) * q2 + psi3(p) * q3 + psi4(p) * q4;
			}

			throw new Exception($"ERROR! Couldn't find finite element for point {p.X}, {p.Y}");
		}

		public double GetValueB(Point p)
		{
			var e = FindElement(p);

			if (e != null)
			{
				double[,] localG1 = new double[4, 4]
				{
					{  2, -2,  1, -1 },
					{ -2,  2, -1,  1 },
					{  1, -1,  2, -2 },
					{ -1,  1, -2,  2 }
				};

				double[,] localG2 = new double[4, 4]
				{
					{  2,  1, -2, -1 },
					{  1,  2, -1, -2 },
					{ -2, -1,  2,  1 },
					{ -1, -2,  1,  2 }
				};

				Point p1 = Mesh.Points[e[0]];
				Point p2 = Mesh.Points[e[1]];
				Point p3 = Mesh.Points[e[2]];

				double x1 = p1.X;
				double x2 = p2.X;
				double y1 = p1.Y;
				double y2 = p3.Y;

				double hx = p2.X - p1.X;
				double hy = p3.Y - p1.Y;

				double B2 = 0.0;

				double mes = hx * hy;

				for (int i = 0; i < FEMParameters.BasisSize; i++)
					for (int j = 0; j < FEMParameters.BasisSize; j++)
						B2 += (hy * localG1[i, j] / hx + hx * localG2[i, j] / hy) / 6.0 * Q[e[i]] * Q[e[j]];

				return Math.Sqrt(B2 / mes);

				// Второй способ
				//Point p1 = Mesh.Points[e[0]];
				//Point p2 = Mesh.Points[e[1]];
				//Point p3 = Mesh.Points[e[2]];

				//double x1 = p1.X;
				//double x2 = p2.X;
				//double y1 = p1.Y;
				//double y2 = p3.Y;

				//double hx = p2.X - p1.X;
				//double hy = p3.Y - p1.Y;

				//double X1 = (x2 - p.X) / hx;
				//double X2 = (p.X - x1) / hx;
				//double Y1 = (y2 - p.Y) / hy;
				//double Y2 = (p.Y - y1) / hy;

				//double q1 = Q[e[0]];
				//double q2 = Q[e[1]];
				//double q3 = Q[e[2]];
				//double q4 = Q[e[3]];

				//double Bx, By;

				//Console.WriteLine(Bx = (X1 * (q3 - q1) + X2 * (q4 - q2)) / hy);
				//Console.WriteLine(By = -(Y1 * (q2 - q1) + Y2 * (q4 - q3)) / hx);

				//return Math.Sqrt(Bx * Bx + By * By);
			}

			throw new Exception($"ERROR! Couldn't find finite element for point {p.X}, {p.Y}");
		}

		FiniteElement FindElement(Point p)
		{
			FiniteElement element = null;
			foreach (var e in Mesh.Elements)
			{
				Point p1 = Mesh.Points[e[0]];
				Point p2 = Mesh.Points[e[1]];
				Point p3 = Mesh.Points[e[2]];

				double x1 = p1.X;
				double x2 = p2.X;
				double y1 = p1.Y;
				double y2 = p3.Y;

				if (p.X >= x1 && p.X <= x2 && p.Y >= y1 && p.Y <= y2)
				{
					element = e;
					break;
				}
			}

			return element;
		}
	}

	public struct NonlinearProblemInfo
	{
		public Mesh LinearMesh;
		public Mesh NonlinearMesh;
		public SolverTypes SolverType;
	}

	class NonlinearProblem
	{
		public double[] Q { get; set; } = null;
		public Mesh LinearMesh { get; set; } = null;
		public Mesh Mesh { get; set; } = null;
		public ISolver Solver { get; set; } = null;

		public double Eps { get; set; } = 1.0e-12;
		public int MaxIter { get; set; } = 30;
		public int CurrentIter { get; set; } = 0;
		public double Diff { get; set; } = 1.0;
		public double W { get; set; } = 0.5;

		double[] prevQ;
		PortraitBuilder PB = null;
		NewtonSLAEBuilder SB = null;

		public NonlinearProblem(NonlinearProblemInfo info)
		{
			LinearMesh = info.LinearMesh;
			Mesh = info.NonlinearMesh;

			PB = new PortraitBuilder(Mesh);
			SB = new NewtonSLAEBuilder(Mesh);

			switch (info.SolverType)
			{
				case SolverTypes.LOSLLT:
					{
						Solver = new LOSLLT();
						break;
					}
				case SolverTypes.LOSLU:
					{
						Solver = new LOSLU();
						break;
					}

				default:
					{
						Solver = new LOSLU();
						break;
					}
			}
		}

		public void Solve()
		{
			Problem problem = new Problem(new ProblemInfo { Mesh = LinearMesh, SolverType = Solver.SolverType });
			problem.Solve();

			Console.WriteLine("Az: " + problem.GetValueA(new Point(-1.53E-02, 3.50E-03)));
			Console.WriteLine("|B|: " + problem.GetValueB(new Point(-1.53E-02, 3.50E-03)));

			Q = problem.Q;

			MatrixPortrait MP = new MatrixPortrait();
			PB.Build(MP);
			IMatrix A = new SymmetricSparseMatrix(Mesh.NodeCount, MP);
			double[] B = new double[Mesh.NodeCount];

			CurrentIter = 0;
			Diff = 1.0;
			while (CurrentIter < MaxIter && Diff >= Eps)
			{
				SB.Build(A, B, Q);
				prevQ = Q;
				Q = Solver.Solve(A, B);

				for (int i = 0; i < Q.Length; i++)
					Q[i] = Q[i] * W + (1 - W) * prevQ[i];

				SB.Build(A, B, Q);

				double[] Aq = new double[Q.Length];
				A.Multiply(Q, Aq);

				for (int i = 0; i < Aq.Length; i++)
					Aq[i] -= B[i];

				Console.WriteLine($"Iter: {CurrentIter}, Diff: {Diff}");

				Diff = VectorUtils.Norm(Aq) / VectorUtils.Norm(B);
				CurrentIter++;
				
				A.Clear();
				Array.Fill(B, 0.0);
			}
		}

		public double GetValueA(Point p)
		{
			var e = FindElement(p);

			if (e != null)
			{
				Point p1 = Mesh.Points[e[0]];
				Point p2 = Mesh.Points[e[1]];
				Point p3 = Mesh.Points[e[2]];

				double x1 = p1.X;
				double x2 = p2.X;
				double y1 = p1.Y;
				double y2 = p3.Y;

				double hx = Math.Abs(p2.X - p1.X);
				double hy = Math.Abs(p3.Y - p1.Y);

				Func<Point, double> psi1 = (Point p) => (x2 - p.X) * (y2 - p.Y) / (hx * hy);
				Func<Point, double> psi2 = (Point p) => (p.X - x1) * (y2 - p.Y) / (hx * hy);
				Func<Point, double> psi3 = (Point p) => (x2 - p.X) * (p.Y - y1) / (hx * hy);
				Func<Point, double> psi4 = (Point p) => (p.X - x1) * (p.Y - y1) / (hx * hy);

				double ps1 = psi1(p);
				double ps2 = psi2(p);
				double ps3 = psi3(p);
				double ps4 = psi4(p);

				double q1 = Q[e[0]];
				double q2 = Q[e[1]];
				double q3 = Q[e[2]];
				double q4 = Q[e[3]];

				return psi1(p) * q1 + psi2(p) * q2 + psi3(p) * q3 + psi4(p) * q4;
			}

			throw new Exception($"ERROR! Couldn't find finite element for point {p.X}, {p.Y}");
		}

		public double GetValueB(Point p)
		{
			var e = FindElement(p);

			if (e != null)
			{
				double[,] localG1 = new double[4, 4]
				{
					{  2, -2,  1, -1 },
					{ -2,  2, -1,  1 },
					{  1, -1,  2, -2 },
					{ -1,  1, -2,  2 }
				};

				double[,] localG2 = new double[4, 4]
				{
					{  2,  1, -2, -1 },
					{  1,  2, -1, -2 },
					{ -2, -1,  2,  1 },
					{ -1, -2,  1,  2 }
				};

				Point p1 = Mesh.Points[e[0]];
				Point p2 = Mesh.Points[e[1]];
				Point p3 = Mesh.Points[e[2]];

				double x1 = p1.X;
				double x2 = p2.X;
				double y1 = p1.Y;
				double y2 = p3.Y;

				double hx = p2.X - p1.X;
				double hy = p3.Y - p1.Y;

				double B2 = 0.0;

				double mes = hx * hy;

				for (int i = 0; i < FEMParameters.BasisSize; i++)
					for (int j = 0; j < FEMParameters.BasisSize; j++)
						B2 += (hy * localG1[i, j] / hx + hx * localG2[i, j] / hy) / 6.0 * Q[e[i]] * Q[e[j]];

				return Math.Sqrt(B2 / mes);

				// Второй способ
				//Point p1 = Mesh.Points[e[0]];
				//Point p2 = Mesh.Points[e[1]];
				//Point p3 = Mesh.Points[e[2]];

				//double x1 = p1.X;
				//double x2 = p2.X;
				//double y1 = p1.Y;
				//double y2 = p3.Y;

				//double hx = p2.X - p1.X;
				//double hy = p3.Y - p1.Y;

				//double X1 = (x2 - p.X) / hx;
				//double X2 = (p.X - x1) / hx;
				//double Y1 = (y2 - p.Y) / hy;
				//double Y2 = (p.Y - y1) / hy;

				//double q1 = Q[e[0]];
				//double q2 = Q[e[1]];
				//double q3 = Q[e[2]];
				//double q4 = Q[e[3]];

				//double Bx, By;

				//Console.WriteLine(Bx = (X1 * (q3 - q1) + X2 * (q4 - q2)) / hy);
				//Console.WriteLine(By = -(Y1 * (q2 - q1) + Y2 * (q4 - q3)) / hx);

				//return Math.Sqrt(Bx * Bx + By * By);
			}

			throw new Exception($"ERROR! Couldn't find finite element for point {p.X}, {p.Y}");
		}

		FiniteElement FindElement(Point p)
		{
			FiniteElement element = null;
			foreach (var e in Mesh.Elements)
			{
				Point p1 = Mesh.Points[e[0]];
				Point p2 = Mesh.Points[e[1]];
				Point p3 = Mesh.Points[e[2]];

				double x1 = p1.X;
				double x2 = p2.X;
				double y1 = p1.Y;
				double y2 = p3.Y;

				if (p.X >= x1 && p.X <= x2 && p.Y >= y1 && p.Y <= y2)
				{
					element = e;
					break;
				}
			}

			return element;
		}
	}
}