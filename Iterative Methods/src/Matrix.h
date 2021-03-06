#pragma once
#include <fstream>
#include <string>

using namespace std;

const int maxiter = 50000;
const double eps = 1e-10;

struct Matrix
{
	double *D, *L1, *L2, *U1, *U2;
	int N, m; 
};

void ReadMatrix(Matrix& M);														// ������ �������
void ReadX0(double* x0, int& N);												// ������ ���������� �����������
void ReadF(double* f, int& N);													// ������ ������ �����
double Multiply(Matrix& M, double* vec, int i);									// ��������� ������� �� ������
double Norm(double* vec, int& N);												// ����� �������
int Jacobi(Matrix& M, double* f, double* x0, double* x1, double* r, double w);	// ����� �����
int Zeidel(Matrix& M, double* f, double* x0, double* r, double w);				// ����� �������
