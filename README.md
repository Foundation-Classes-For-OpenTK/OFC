# OFC
Open Foundation Classes for OpenTK

This site is not associated with OpenTK project. 

This repository holds a set of c# class wrappers which wrap the OpenTK GL API interface (which is a thin shim on top of OpenGL) into more useable c# classes.  

These make drawing objects to GL much easier to perform.

The classes cover the major GL objects: Buffer, Vertexes, Program, Shaders, Textures, Uniforms.

It has a render list which allows the rendering of all objects to be executed in one call.

A 3dcontroller is present to allow you to move the eye/target position through the world.

A basic set of shape factories allow some basic shapes to be turned into vertex lists.

Also present is a WINFORM style set of classes to allow a basic UI to be presented in GL, allowing creation of Forms, Buttons, Labels, Comboboxes, drop down lists, panels, etc.

This code was written for the EDDiscovery project and so is orientated for that use.

The code is in development and more will be added as we use this code in EDD.

The code is licensed under the Apache License, Version 2.0 (the "License"); you may not use this code except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0. Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.



