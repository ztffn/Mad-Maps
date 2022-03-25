
**Welcome To Mad Maps!**

Mad Maps is a powerful, integrated collection of tools to solve all of your Unity terrain pipeline needs. It is a terrain pipeline that is non-destructive, extensible, flexible, fast and modular. It's been battle-tested for the past 2 years in a professional studio environment, used for creating massive open world levels.

It contains 3 integrated toolsets:

The **Terrain Wrapper**: a layer system for the Unity terrain. Useful for seamlessly integrating with existing procedural generation solutions (see below), allowing you to combine procedural generation with handcrafted polish. Organise layers, snapshot and store terrains, create procedural filters, and more.

**World Stamps**: for splitting complex levels up into manageable chunks, and easily modify, store and reuse areas of your world. It's a copy/paste functionality for your entire world.

**Road Networks**: a complete road tool, for easily creating spline-based mesh, object and terrain modifiers. Works seamlessly with the Terrain Wrapper and World Stamp system for a non-destructive workflow. Paint complex road networks, deform the terrain, place/remove objects, and more.

For support and any questions, please contact [sean@bankrollstudios.com](mailto:sean@bankrollstudios.com)




**Contents**

[The Terrain Wrapper 3](#__RefHeading___Toc105_1683821825)

[Removals 3](#__RefHeading___Toc110_1683821825)

[The Stencil 3](#__RefHeading___Toc112_1683821825)

[Layer Commands 4](#__RefHeading___Toc114_1683821825)

[Procedural Layers 4](#__RefHeading___Toc119_1683821825)

[Splats and Details 4](#__RefHeading___Toc121_1683821825)

[Info 5](#__RefHeading___Toc123_1683821825)

[World Stamps 6](#__RefHeading___Toc177_1683821825)

[Single Instance Editing 7](#__RefHeading___Toc179_1683821825)

[Previewing and Stamping 7](#__RefHeading___Toc181_1683821825)

[Channels 8](#__RefHeading___Toc183_1683821825)

[Heightmap 8](#__RefHeading___Toc185_1683821825)

[Trees 8](#__RefHeading___Toc187_1683821825)

[Objects 8](#__RefHeading___Toc189_1683821825)

[Splats 9](#__RefHeading___Toc191_1683821825)

[Details 9](#__RefHeading___Toc193_1683821825)

[Road Network 10](#__RefHeading___Toc195_1683821825)

[Connections 11](#__RefHeading___Toc197_1683821825)

[Modifying Nodes 12](#__RefHeading___Toc199_1683821825)

[Intersections 13](#__RefHeading___Toc201_1683821825)

[Configuration 15](#__RefHeading___Toc203_1683821825)

[Tools 15](#__RefHeading___Toc205_1683821825)

[World Stamp Creation 17](#__RefHeading___Toc207_1683821825)

[Starting Out 17](#__RefHeading___Toc209_1683821825)

[Capturing 17](#__RefHeading___Toc211_1683821825)

[Previewing and Configuring 17](#__RefHeading___Toc213_1683821825)

[Heightmap 17](#__RefHeading___Toc215_1683821825)

[Splats, Details and Trees 17](#__RefHeading___Toc217_1683821825)

[Objects 18](#__RefHeading___Toc219_1683821825)

[Roads 19](#__RefHeading___Toc221_1683821825)

[Mask 19](#__RefHeading___Toc223_1683821825)

[Finalizing Your Creation 19](#__RefHeading___Toc225_1683821825)




======

The Terrain Wrapper
===================

The Terrain Wrapper component is a system that allows you to manage various layers of terrain information. It is placed on your [Terrain](https://docs.unity3d.com/ScriptReference/Terrain.html) object. The inspector contains 4 tabs. The first shows a list of the layers currently making up the terrain. There are currently 2 different types of layers:

These layers contain various types of information that go into building a terrain in a world, including heightmap information, splat maps, detail maps, trees and objects. Layers can be selected, dragged around and re-ordered, and are executed from the bottom upwards similar to layers in an image editing program. You can inspect the information within a Terrain Layer by clicking the eye icon (  [![EyeOpenIcon.png](http://lrtw.net/madmaps/images/thumb/d/dc/EyeOpenIcon.png/18px-EyeOpenIcon.png)](http://lrtw.net/madmaps/index.php?title=File:EyeOpenIcon.png)) next to the information you want to see. You can also clear any piece of information by clicking the bin icon (  [![BinIcon.png](http://lrtw.net/madmaps/images/e/e6/BinIcon.png)](http://lrtw.net/madmaps/index.php?title=File:BinIcon.png)). To apply the current stack of layers to the terrain, click the Reapply All button below the list of layers.

Removals
--------

Whenever we place an object or a tree in a Terrain Layer, we give it a unique ID that can be used to reference it. Sometimes, we want to remove objects and trees that have been placed on a base layer in a layer above. Consider placing a [World Stamp](http://lrtw.net/madmaps/index.php?title=World_Stamp) of a town within a forest, and wanting that stamp to remove any forest trees and rocks it encounters. To do this, we store a list of removals, which is a list of IDs of objects that exist below the layer (in other earlier layers), that the layer will remove from the final terrain.

This is important to remember when stamping or regenerating trees and objects - whenever you regenerate trees or objects in a layer, any removals of those objects above those layers will lose their reference, as these are new trees with new IDs that you have made.

As a general rule, when you change a Terrain Layer, you should re-stamp and regenerate any layers above. For instance, in the inspector screenshot to the right, if we were to change the _Farms_ layer, we should regenerate the [Road Network](http://lrtw.net/madmaps/index.php?title=Road_Network) to refresh the Road Network layer, and re-stamp any stamps writing to the _Towns_ layer. The top layer is a Procedural Layer and so does not need to be regenerated. The bottom two layers will also not need to be regenerated, as they are not dependent on the _Farms_ layer.

The Stencil
-----------

[![](http://lrtw.net/madmaps/images/thumb/3/31/Stencil1.PNG/300px-Stencil1.PNG)](http://lrtw.net/madmaps/index.php?title=File:Stencil1.PNG)

An example of a stencil previewed in the scene

When a layer is executed, it combines its information with the layers below based on the Stencil, which is equivalent to a mask. You can see the stencil in the scene by clicking the Terrain icon (  [![TerrainIcon.png](http://lrtw.net/madmaps/images/6/6d/TerrainIcon.png)](http://lrtw.net/madmaps/index.php?title=File:TerrainIcon.png)) next to it, or in a window by clicking the eye icon (  [![EyeOpenIcon.png](DocumentationWorkingFIle_html_eddc34b8dcd7b3f2.png)](http://lrtw.net/madmaps/index.php?title=File:EyeOpenIcon.png)). This can be useful for debugging multiple stamps blending together - each stamp is assigned a different color in the stencil, so you can explicitly see where a stamp is modifying the terrain.

Layer Commands
--------------

When you have a layer selected, you can modify it with a few commands as shown at the bottom of the inspector.

**Capture From Terrain** will collect all information it can currently from the terrain - including heightmap data, splat data, detail data, prefabs, and trees - and write it into the selected layer.

**Apply To Terrain** will apply only this layer to the terrain. It is equivalent to disabling all other layers but this one, and clicking "Reapply All".

**Clear** will delete all information on this layer.

Procedural Layers
-----------------

Procedural Layers are places where you can apply rules to your terrain. For instance, you can tell the Terrain Wrapper to remove all objects sitting on a slope that's steeper than a given gradient, or to raycast your trees to make sure that none are colliding with objects. It's easy to write small snippets of code that can modify the state of the terrain information at any point by writing your own Procedural Layer Component, or use the many components that come built in.

Splats and Details
------------------

[![TerrainWrapperSplats.PNG](http://lrtw.net/madmaps/images/7/77/TerrainWrapperSplats.PNG)](http://lrtw.net/madmaps/index.php?title=File:TerrainWrapperSplats.PNG)

The TerrainWrapper system manages the detail and splat prototypes of a terrain for you. The reason for this is to make it so that both \[[Detail Prototypes](https://docs.unity3d.com/ScriptReference/DetailPrototype.html)\] and \[[Splat Prototypes](https://docs.unity3d.com/ScriptReference/SplatPrototype.html)\] can easily be reference across scenes by multiple stamps and layers, and resolved into a manageable amount of prototypes when combined. Instead of specifying these things within a single terrain, we instead create an asset that represents the splat or detail layer. We call these objects a Detail Prototype Wrapper and a Splat Prototype Wrapper respectively.

To create a Wrapper asset, right click in the Project window and go to Create > Mad Maps > Terrain > Detail Prototype OR Splat Prototype. These Wrappers are identical in configuration to normal Unity Terrain wrappers.

Stamping a [World Stamp](http://lrtw.net/madmaps/index.php?title=World_Stamp) to a layer will automatically add the required Detail and Splat Wrappers. You can see what wrappers write to what layers. To explicitly add a Wrapper to either the splats or the details, press the + button. To remove a selected wrapper, press the - button. To refresh the terrain - that is, set the wrappers to be the prototypes on the terrain - press the refresh button.

If you are painting splats or details to be captured by a stamp, it is highly recommended that you add splats and details this way. This will make capturing and combining these layers of information much, much easier later on. It also means that you can modify a single asset and see your changed splats and details reflected throughout your project.

Info
----

[![TerrainWrapperInfo.PNG](DocumentationWorkingFIle_html_6cdbebab4d46a4f5.png)](http://lrtw.net/madmaps/index.php?title=File:TerrainWrapperInfo.PNG)

The Info tab shows some settings about the Terrain Wrapper's behavior, as well as information about what is currently being written to the terrain.

The **Compute Shaders** checkbox enables and disables GPU processing for the Mad Maps system. In some rare cases, such as when you do not have a graphics card or your graphics card is very weak, you may prefer to use CPU processing. In most other cases, GPU processing will be a lot faster.

The **Write Heights/Splats/Trees/etc** checkboxes allow you to enable or disable the TerrainWrapper writing to certain channels of the terrain.

Below this, you can see the compound data. This is the combined data of all of your layers that is actually written to your terrain.

World Stamps
============

[![WorldStampGUI.png](DocumentationWorkingFIle_html_225f4f0aeca47ba.png)](http://lrtw.net/madmaps/index.php?title=File:WorldStampGUI.png)  

World Stamps are objects that contain world information, that can be stamped onto a Terrain that is using the Terrain Wrapper. Stamps write their information into a Terrain Layer, and multiple stamps can write to the same layer. World Stamps are captured from a terrain using the World Stamp Creator. They contain several channels of data - heightmap information, trees, objects, splat maps and detail maps. You can disable these channels if you want to only write certain types of data from the World Stamp by unticking the checkbox beside that channels header. Each channel has some options as to how the information is blended with the existing terrain.

When applying a stamp, it is only aware of the terrain information below it in the Terrain Wrapper layers, and of stamps that have already been applied in the same layer.

Single Instance Editing
-----------------------

When you only have one World Stamp selected, you will see information about that stamp and be able to preview the data within the stamp using the eye icon (  [![EyeOpenIcon.png](DocumentationWorkingFIle_html_eddc34b8dcd7b3f2.png)](http://lrtw.net/madmaps/index.php?title=File:EyeOpenIcon.png)).

You can also edit the mask of the stamp by clicking the Edit Mask button, which will enable the painting tool. If the stamp is a prefab, this will create a version of the mask just for this instance in the scene. This is useful for tweaking the mask of a stamp, while not changing every instance. When you have finished editing the mask, click Finish Editing to close the painting tool. To revert this instance's mask to the prefab's, click the Revert To Prefab button. If you want to commit your altered mask to the stamp's prefab, click the Write Mask To Prefab button.

Previewing and Stamping
-----------------------

To actually stamp your World Stamps, we have two options. We can stamp a specific layer with the Stamp Layer X button, where X is the name of the layer. This will go and find every stamp that writes to that layer, and stamp it. You must stamp at least every stamp on a single layer, as stamps can interact with each other. You can also Stamp All Layers - this will execute every World Stamp in the scene.

Multiple stamps are executed first in layer order (earlier stamps first) and then in order of hierarchy (higher stamps first).

Recalculate Terrain - By default, stamping a World Stamp will trigger the TerrainWrapper to reapply all of it's layers, so you can see the result of the stamping. If you don't want this, untick this checkbox.

Gizmos Enabled - This enables/disables the in-scene gizmos for the stamp (the box that shows the area of the stamp). You can disable all gizmos of all stamps in the scene by clicking All Off.

Gizmo Color - Here you can modify the color of the gizmo for this stamp, which is useful for categorising and seeing what stamps are where within your scene.

Size - Here you can specify the size of the stamp. This can also be altered using the in-scene handles. To revert the stamp back to it's native size, click Reset.

Snap To Terrain Height - This specifies whether you want the stamp to snap to the terrain's height. Note that the height that this will snap to is the height at that point in the layer - if you change the height in a later layer than the stamp writes to, it will not stamp to that height. You can specify an offset to the snapping value with the Snap To Terrain Height Offset field.

Layer Name - Here you specify the name of the layer that this stamp will write into. When you stamp the World Stamp, it will find the Terrain Wrapper object, and look for a layer with this name. If it cannot find it, it will make a new layer with this name.

Preview Enabled - Here you can enable/disable a preview of the stamp's heightmap. This is useful for approximate placement of stamps. To disable the preview of all stamps in the scene, click the All Off button.

Priority - If you want, you can specify a explicit priority of the stamp to change when it is executed. Generally, it is better to set this priority using hierarchy order.

Channels
--------

### Heightmap

Heightmaps can be blended together with several different modes - Set, Add, Max, Min and Average. The value that is compared is the height at that moment in the layer, not necessarily the final height of the terrain.

Min and Max Blend Modes - These blend modes are special, in that they involve a test (e.g. was the height of the terrain higher/lower than that of the stamp). Where this test fails, the stamp won't write to the stencil (see Terrain Wrapper for an explanation of the stencil). This means it won't change the heightmap, place trees, objects, splats or details. If you don't want this behaviour, make sure to set fields involving the stencil (see below).

Height Offset - Here you can specify an offset to the heightmap.

### Trees

Remove Trees - This specifies whether to remove trees that already exist on the terrain, that are covered by the stamp.

Stencil Trees - This specifies whether to check the stencil (see above) to see if we should place a given tree or not.

### Objects

Remove Objects - This specifies whether to remove objects that already exist on the terrain, that are covered by the stamp.

Stencil Objects - This specifies whether to check the stencil (see above) to see if we should place a given object or not.

Override Object Relative Mode - Here you can override the Object Relative Mode of the stamp. For an explanation of what this means, see the World Stamp Creator section.

### Splats

Stencil Splats - Here you can define if you want the splats to be affected by the stencil. If this is unticked, they will only be masked out by the stamp's mask.

Similar to heightmaps, stamps have several different blend modes that change how they mix with the existing splats. You can also disable any splat prototypes if you want.

### Details

Again similar to heightmaps and splats, the detail channel has several different blend modes available.

Detail Boost - You can define here a multiplier for the detail maps.

Similar to splat maps, you can disable any detail prototypes you wish.




Road Network
============

[![RoadNetworkConnections.PNG](DocumentationWorkingFIle_html_d510c6897bb44fa7.png)  
](http://lrtw.net/madmaps/index.php?title=File:RoadNetworkConnections.PNG)  

The Road Network is a system for building roads and other spline-based objects. This tool can be used for:

*   Distorting a mesh along a spline, for instance roads, barriers that run along the side of roads, pipes, rope and wires

*   Alter the terrain heightmap in any way. For instance, to fit underneath the road, or dip and form a riverbank.

*   Place or remove trees, objects, splats, and details along a spline with a given distance and falloff.


The Road Network interacts with the [Terrain Wrapper](http://lrtw.net/madmaps/index.php?title=Terrain_Wrapper) system by writing a lot of it's information into a layer. It will find or create a layer called "Road Network" on your Terrain Wrapper, and write any modifications to any related information.

To create a new Road Network in your scene, right click in your hierarchy and select Create > 3D Object > Road Network. To start editing the Network, select it and click "Open Editor Window" from the inspector. This will open a new window shown to the right. The Road Network window has 3 tabs - Connections, Intersections and Configuration.

Road Networks are built by placing Nodes and connecting them together. These connections can then have [Connection Configurations](http://lrtw.net/madmaps/index.php?title=Connection_Configuration) applied to them. Connection Configurations are collections of Connection Components, each of which do some task along a connection (for instance, distorting a mesh, removing trees, that kind of thing). When you Rebake the Road Network (done by clicking the button at the bottom of the window), it will go and execute every component on every connection in your Network.

Connections
-----------

[![](DocumentationWorkingFIle_html_8f776b556eac305a.png)](http://lrtw.net/madmaps/index.php?title=File:PlaceNewConnectionSceneView.png)

Creating a new Node off an existing one

When you have this tab open in the window, you can place and connect nodes. To paint with a given [Connection Configuration](http://lrtw.net/madmaps/index.php?title=Connection_Configuration), you can select it in the Configuration field. Previous Connection Configurations that you've used are shown in the list above for easy re-selection. To remove a Connection Configuration from this list, click the bin icon (  [![BinIcon.png](DocumentationWorkingFIle_html_47cdd3d0b1d21235.png)](http://lrtw.net/madmaps/index.php?title=File:BinIcon.png)).

To start placing a new node, hold down the CONTROL key. You will see a preview under your cursor of where the node will be placed. To actually place the node, while still holding the CONTROL key, click the MIDDLE MOUSE BUTTON. You will see a node appear.

To select a node, hover over it with your mouse and click your MIDDLE MOUSE BUTTON.

If you place a new node with an existing node already selected, those two nodes will be connected. Alternatively, you can connect two existing nodes by just selecting one, holding down the CONTROL key, and MIDDLE MOUSE BUTTON clicking the other existing node.

### Modifying Nodes

Each node is a Gameobject, and you can move them around as you would any other GameObject. When you have a node selected, you will be able to modify some properties of it in the inspector, which will affect how it will connect with other nodes. A brief summary of your options is below.

*   **Override Curviness** - The curviness of your spline is how basically how quickly your spline will change direction. A low curviness means your spline will change direction very quickly from node to node, whereas a high curviness will mean the spline will be smoother. A default curviness is defined in a connection's configuration, but if you wish you can override this here by ticking this checkbox and entering your own value.

*   **Snapping Mode** - A node can be set to snap to the ground below it in a few different ways.

    *   **None**: The node will not snap to the ground

    *   **Wrapper**: The node will snap to the height given in the Terrain Wrapper, on the Road network layer. This means that if you have height differences in layers above the Road Network layer, they won't affect the snapping.

    *   **Terrain**: The node will snap to the height given by the final terrain height. Note that if the connection modifies the terrain height itself, this can conceivably lead to recursive snapping as the node snaps to the new height that it itself has created.

    *   **Raycast**: The node will snap to the collision point of a raycast, with the specified Layer Mask.


*   **Is Explicit Control** - The control of a node is the same as a control in any spline tool. It tells a node how it should curve to connect to others. For the most part, you want nodes to automatically determine their control to allow for smooth connections. However, sometimes you will want to specify an explicit control, to create things like intersections. To do this, check this tickbox, and enter a Vector3 of the control. You will be able to see a dotted green line that shows the explicit control.

*   **Offset** - You can offset the actual node point from the origin of the GameObject with this field.

*   **Seed** - Some connection components have randomness to them, which is derived from this seed. If you want to randomly set this seed, click the button to the right of this field.


Intersections
-------------

[![](DocumentationWorkingFIle_html_23ac05afd80e54c.png)](http://lrtw.net/madmaps/index.php?title=File:InsertIntersection.png)

Inserting an Intersection Into a Node

[![RoadNetworkIntersections.PNG](DocumentationWorkingFIle_html_67519822ff53638a.png)](http://lrtw.net/madmaps/index.php?title=File:RoadNetworkIntersections.PNG)

In Mad Maps, road intersections are simply prefabs with explicitly placed Node components on them. You can create whatever shaped intersections you wish in a 3D tool, and then explicitly place any nodes you want on it that your roads can be connected to. To create an intersection, simply make a prefab with several Node components within it.

There are two ways to create intersections within a Road Network. Firstly, you can just treat intersections as normal prefabs and place them within your scene, as a child object of the Road Network. A Road Network owns all Nodes that are children of it. To force the Road Network to recognize any new nodes added by dragging a new prefab into it, you can click the "Force Refresh" button in the Configuration tab.

The road network can also attempt to automatically insert an intersection for ease of use. This will replace a selected node with the intersection prefab, and attempt to find a best fit for the new connections. This can be a much faster and easier way to insert intersections. To automatically insert an intersection, select a prefab with at least one Node component in the Intersection field. Then, select the node you want to insert into. You will see a preview of the node positions in the scene. You can rotate the inserted node with the Rotation field. When you're happy, click the "Insert Intersection Into Node" button.

Configuration
-------------

[![RoadNetworkConfiguration.PNG](DocumentationWorkingFIle_html_5441033991f637d8.png)](http://lrtw.net/madmaps/index.php?title=File:RoadNetworkConfiguration.PNG)

**Spline Resolution** - This defines the global road network spline resolution. It is a measure of "points per meter". Lower values will result in lower resolution splines, and vice versa.

**Node Preview Size** - This is the size of the node gizmos that appear in the scene.

**Recalculate Terrain** - By default, the Road Network will trigger a "Reapply All" operation on the [Terrain Wrappers](http://lrtw.net/madmaps/index.php?title=Terrain_Wrapper) it modifies. If you don't want this to happen, you can untick this checkbox.

**Ignored Connection Component Types** - Here you can globally disable certain types of Connection Component, which will not run the next time you click "Rebake".

### Tools

**Snap To Surface** - This command will force all nodes to resnap to the surface of the terrain.

**Force Refresh** - This command will make the Road Network find any new nodes, as well as resolve any issues and invalid states with existing nodes.

**Delete all DataContainer Objects** - When some components execute, they create sub-objects of the nodes. For instance, the Create Render Mesh component will create several renderers. This command will destroy all of these objects within the network.

**Create Stripped Copy** - This will create a flattened version of the Road Network without any scripts and a simpler hierarchy. This is useful if you want to build a scene but not include the road metatdata.

World Stamp Creation
====================

The World Stamp Creator is a tool used to capture segments of your world and turn them into World Stamps. You can open the World Stamp Creator by going to Tools > Mad Maps > World Stamp Creator.

Starting Out
------------

The Creator takes a target Unity Terrain as it's first variable. This terrain does **not** require a Terrain Wrapper component, but neither will one break anything. The first thing you will need to do is specify the area of the terrain that you want to capture. In the Scene View you will see two movement handles on the terrain, that can be moved around to specify a box. This box is your capturing bounds, and everything within this box on the terrain will be captured into your World Stamp.

When you are happy with the area you have defined, you can click the "lock" icon up the top left. This will hid the movement handles, so you don't accidentally move them later on.

Capturing
---------

Now, you are ready to take a snapshot of your level. To do this, click the Capture button. If this button is ever red, that means that something has changed within the settings (such as the bounds changing) and the information needs to be captured again.

Previewing and Configuring
--------------------------

Once the capture process is complete, you'll be able to preview the information that you've captured on each of the different layers by clicking the "Preview" button next to the respective layer. You can expand each layer to configure the data within. Here's a brief summary of the settings:

### Heightmap

*   **Auto Zero Level** - The Zero Level is the ground level of the stamp. By default, this is set to the lowest point in the captured heightmap.

*   **Zero Level** - this is visible if Auto Zero Level is unticked, and allows you to specify an explicit zero level. This is useful in scenarios where you want to capture stamps with depressions into the ground, as you will want the zero level of the stamp to be above the lowest point.


### Splats, Details and Trees

For these categories, you can choose to ignore certain Splat Prototypes, Detail Prototypes and Tree Prototypes from capture. Note that this means these Prototypes will not be written into the stamp at all and the information will not be available later. If you want to have the information within the stamp, but sometimes not use it, then you can capture those Prototypes and simply disable them when it comes time to actually stamp them.

### Objects

All objects captured in World Stamps need to be prefabs. Per instance changes to prefab components will not be recorded, as only the transform information and the prefab reference are stored.

[![](DocumentationWorkingFIle_html_7b5e1bbe7bb5589b.png)](http://lrtw.net/madmaps/index.php?title=File:RelativeModeExplanation.png)

Object Relative Modes

*   **Layer Mask** - You can define what layers to capture objects from if you wish.

*   **Relative Mode** - The Object Relative Mode allows you to choose between two different ways of storing object positions, both with pros and cons. When we store an object's height within a World Stamp, we can either store it as the distance from the origin of the stamp to the object, or from the ground to the object.

    *   **Relative To Stamp** - This means that when you stamp an object down, it's height will be stored as a distance from the stamp's origin. This is better for stamps with closely fitting prefabs, such as man-made structures, where you want exact placement of objects. However, sometimes this will mean that you might have objects sink below ground. For instance, in the image to our right we see a stamp of a hill with 4 rocks on it. When objects are placed relative to the stamp, two rocks on either side sit below the terrain.

    *   **Relative to Terrain** - This will mean that the object heights are stored relative to their distance from the terrain. This can work better for natural features that you may always want to sit on the ground level, no matter how the stamp heights get stamped.


### Roads

The World Stamp Capture tool can also automatically create a stripped down, or fully copied, duplicate of a road network within the World Stamp. This is useful for creating World Stamps with Road Networks embedded within them, that can be linked up with other Road Networks once stamped.

### Mask

A World Stamp's mask tells it how it should blend in with the rest of the level when stamped. It is similar to a mask in image editing software. For instance, if you were capturing a hill, you would probably not be interested in the flat area around the hill, and so would want to mask that area out. To edit the mask, click the Edit button. This will bring up the painting interface, where you can paint the mask in the scene.

*   **Reset** - This resets the mask to full, meaning that no area is masked out.

*   **Fill From Zero Level** - See Zero Level explanation above. This fills in the mask everywhere where the height value is the zero level.

*   **Load From Texture** - This allows you to load a stamp mask from an image. The value is taken as the grayscale of the image.


Finalizing Your Creation
------------------------

When you're happy with your configuration, you can now create your World Stamp. Do this by clicking the "Create New World Stamp" button. You can also write new data into an existing World Stamp by selecting it in the given field and clicking "Replace Existing Stamp". However, note that this will not work when embedding Road Networks within a stamp. In that scenario, you must create a new stamp and then override the existing one by replacing the prefab in the project window.

There is also the opportunity to create a World Stamp Template. These are useful tools for saving and restoring World Stamp Capture settings, for capturing stamps identically multiple times.

Once you've made your stamp, you can make it a prefab and use it elsewhere in your project, or just keep it in your scene and use it there.
