/*
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */
#version 450 core

mat4 mat4identity()
{
	return mat4(	1,0,0,0,			// created in row order, and its in memory order.  translation is first three values in last row
					0,1,0,0,
					0,0,1,0,
					0,0,0,1);
}

mat4 mat4zero()
{
	return mat4(	0,0,0,0,
					0,0,0,0,
					0,0,0,0,
					0,0,0,0);
}

mat4 mat4rotateX(float radians)
{
	float cosv = cos(radians);
	float sinv = sin(radians);
	mat4 v = mat4(	1.0,	0,		0,		0,
					0,		cosv,	sinv,	0,
					0,		-sinv,	cosv,	0,
					0,		0,		0,		1);
	return v;
}

mat4 mat4rotateY(float radians)
{
	float cosv = cos(radians);
	float sinv = sin(radians);
	mat4 v = mat4(	cosv,	0,		-sinv,	0,
					0,		1,		0,		0,
					sinv,	0,		cosv,	0,
					0,		0,		0,		1);
	return v;
}

mat4 mat4rotateZ(float radians)
{
	float cosv = cos(radians);
	float sinv = sin(radians);
	mat4 v = mat4(	cosv,sinv,0,0,
					-sinv,cosv,0,0,
					0,0,1,0,
					0,0,0,1);
	return v;
}

mat4 mat4rotateXthenY(float radiansx, float radiansy)		
{
	float cx = cos(radiansx);
	float sx = sin(radiansx);
	float cy = cos(radiansy);
	float sy = sin(radiansy);

	// same result as   rotatex = mat4rotateX(0.7853); rotatey05 = mat4rotateY(0.5); rotatexy = rotatey05 * rotatex;
	// or  Matrix4 yrot05 = Matrix4.CreateRotationY(0.5f);  Matrix4 xyrot = xrotpi4;  xyrot *= yrot05;

	mat4 v = mat4(	cy,0,-sy,0,
					sx*sy,cx,sx*cy,0,
					cx*sy,-sx,cx*cy,0,
					0,0,0,1); 						

	return v;
}

mat4 mat4rotateXthenYthenScale(float radiansx, float radiansy, vec3 scale)		
{
	float cx = cos(radiansx);
	float sx = sin(radiansx);
	float cy = cos(radiansy);
	float sy = sin(radiansy);

	mat4 v = mat4(	cy*scale.x,0,-sy*scale.x,0,
					sx*sy*scale.y,cx*scale.y,sx*cy*scale.y,0,
					cx*sy*scale.z,-sx*scale.z,cx*cy*scale.z,0,
					0,0,0,1); 						

	return v;
}

// produce a translation matrix with x/y rotations, scaling and translation

mat4 mat4rotateXthenYthenScalethenTranslation(float radiansx, float radiansy, vec3 scale, vec3 translation)		
{
	float cx = cos(radiansx);
	float sx = sin(radiansx);
	float cy = cos(radiansy);
	float sy = sin(radiansy);

	mat4 v = mat4(	cy*scale.x,0,-sy*scale.x,0,
					sx*sy*scale.y,cx*scale.y,sx*cy*scale.y,0,
					cx*sy*scale.z,-sx*scale.z,cx*cy*scale.z,0,
					translation.x,translation.y,translation.z,1); 						

	return v;
}


mat4 mat4translation(vec3 translate)
{
	mat4 v = mat4(	1,0,0,0,
					0,1,0,0,
					0,0,1,0,
					translate.x,translate.y,translate.z,1);
	return v;
}

mat4 mat4translation(mat4 tx, vec3 translate)
{
	mat4 v = mat4(	tx[0][0],tx[0][1],tx[0][2],0,		// [row][col]
					tx[1][0],tx[1][1],tx[1][2],0,		
					tx[2][0],tx[2][1],tx[2][2],0,
					translate.x,translate.y,translate.z,1);
	return v;
}

mat4 mat4scalethentranslate(vec3 scale, vec3 translate)
{
	mat4 v = mat4(	scale.x,0,0,0,
					0,	scale.y, 0,0,
					0,	0,	scale.z,0,
					translate.x,translate.y,translate.z,1);
	return v;
}

mat4 mat4scale(float scale)
{
	mat4 v = mat4(	scale,0,0,0,
					0,scale,0,0,
					0,0,scale,0,
					0,0,0,1);
	return v;
}

mat4 mat4scale(vec3 scale)
{
	mat4 v = mat4(	scale.x,0,0,0,
					0,scale.y,0,0,
					0,0,scale.z,0,
					0,0,0,1);
	return v;
}

