/*
 * Carrier and helper classes
 * February 2024
 * Creator: Cansın Canberi
 * 
 * This class represents a Carrier control system for handling motion control in Carriers consisting of 4 axes: X, Y, Z (Translational) and C (Rotational).
 * It facilitates communication with a ModbusClientTCP instance and provides methods for enabling/disabling the carrier,
 * referencing the carrier, reading its position and velocity, setting default speed and acceleration, moving the carrier safely,
 * and moving each axis individually.
 */


using EasyModbus;
using System;
using System.Collections.Generic;
using System.Linq;
public class Vector4D
{
    // Properties with encapsulation
    public float X, Y, Z, C;

    // Parameterized constructor
    public Vector4D(float x, float y, float z, float c)
    {
        X = x;
        Y = y;
        Z = z;
        C = c;
    }

    // Default constructor using the parameterized constructor
    public Vector4D() : this(0, 0, 0, 0) { }
    // Overload the + operator
    public static Vector4D operator +(Vector4D a, Vector4D b)
    {
        return new Vector4D(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.C + b.C);
    }
    // Overload the - operator
    public static Vector4D operator -(Vector4D a, Vector4D b)
    {
        return new Vector4D(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.C - b.C);
    }
    // Overload the * operator
    public static Vector4D operator *(Vector4D vector, float scalar)
    {
        return new Vector4D(vector.X * scalar, vector.Y * scalar, vector.Z * scalar, vector.C * scalar);
    }

    // Overload the / operator
    public static Vector4D operator /(Vector4D vector, float divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Division by zero.");

        return new Vector4D(vector.X / divisor, vector.Y / divisor, vector.Z / divisor, vector.C / divisor);
    }
    // Overload the == operator
    public static bool operator ==(Vector4D left, Vector4D right)
    {
        return  Math.Equals(left.X, right.X) &&
                Math.Equals(left.Y, right.Y) &&
                Math.Equals(left.Z, right.Z) &&
                Math.Equals(left.C, right.C);
    }
    // Overload the != operator
    public static bool operator !=(Vector4D left, Vector4D right)
    {
        return !(left==right);
    }
}

public class Carrier
{
    private ModbusClientTCP client;
    private int xID, yID, zID, cID;
    Dictionary<char, int> idDict = new Dictionary<char, int>();
    public Carrier(ModbusClientTCP client, int xID, int yID, int zID, int cID)
    {
        this.client = client;
        this.xID = xID;
        this.yID = yID;
        this.zID = zID;
        this.cID = cID;
        AccLimits = new Vector4D(100, 100, 100, 100);  
        VelLimits = new Vector4D(100, 100, 100, 100);       
        posLimits = new Vector4D(10000, 10000, 10000, 360); 
        DefVel = new Vector4D(100, 100, 100, 100);          
        DefAcc = new Vector4D(50, 50, 50, 50);           
        HomePos = new Vector4D(0, 0, 0, 0);         
        LoadPos = new Vector4D(100, 0, 0, 0);
        safetyHeight = 1000;
        idDict['x'] = xID;
        idDict['y'] = yID;
        idDict['z'] = zID;
        idDict['c'] = cID;
    }
    public Vector4D AccLimits;
    public Vector4D VelLimits;
    public Vector4D posLimits;
    private Vector4D DefVel;
    private Vector4D DefAcc;
    public Vector4D HomePos;
    public Vector4D LoadPos;
    public float safetyHeight;
    //Get Current Carrier Position
    public Vector4D ReadPos()
    {        
        float x = client.GetPos(xID);
        float y = client.GetPos(yID);
        float z = client.GetPos(zID);
        float c = client.GetPos(cID);
        return new Vector4D(x, y, z, c);
    }
    //Get Current Carrier Velocity
    public Vector4D ReadVel()
    {
        float x = client.GetVel(xID);
        float y = client.GetVel(yID);
        float z = client.GetVel(zID);
        float c = client.GetVel(cID);
        return new Vector4D(x, y, z, c);
    }
    //Enable an individual Carrier axis (x,y,z or c)
    public void Enable(char ID)
    {
        if (idDict.ContainsKey(ID)){
            client.Enable(idDict[ID]);
            while (!client.IsEnabled(idDict[ID]));
        }
    }
    //Enable all carrier axes
    public void Enable()
    {
        client.Enable(xID);
        client.Enable(yID);
        client.Enable(zID);
        client.Enable(cID);
        while (!client.IsEnabled(xID) || !client.IsEnabled(yID) || !client.IsEnabled(zID) || !client.IsEnabled(cID)) ;
    }
    //Disable an individual Carrier axis (x,y,z or c)
    public void Disable(char ID)
    {
        if (idDict.ContainsKey(ID)){
            client.Disable(idDict[ID]);
            while (client.IsEnabled(idDict[ID])) ;
        }
    }
    //Disable the Carrier
    public void Disable()
    {
        client.Disable(xID);
        client.Disable(yID);
        client.Disable(zID);
        client.Disable(cID);
        while (client.IsEnabled(xID) || client.IsEnabled(yID) || client.IsEnabled(zID) || client.IsEnabled(cID)) ;
    }
    //Reference the Carrier (order: z,x,y,c)
    public void Reference()
    {
        Console.WriteLine($"Referencing the system!");
        client.Reference(zID);
        Console.WriteLine($"Z referenced!");
        //blocking
        client.Reference(xID);
        client.Reference(yID);
        client.Reference(cID);
        Console.WriteLine($"X,Y,C referenced!");
    }
    //Reference an individual Carrier axis (x,y,z or c)
    public void Reference(char ID)
    {
        if (idDict.ContainsKey(ID))
        {
            client.Reference(idDict[ID]);
            while (client.IsReferenced(idDict[ID])) ;
        }
    }
    //Check if Enabled
    public bool IsEnabled()
    {
        return (client.IsEnabled(xID) && client.IsEnabled(yID) && client.IsEnabled(zID) && client.IsEnabled(cID));
    }
    //Check if Referenced
    public bool IsReferenced()
    {
        return (client.IsReferenced(xID) && client.IsReferenced(yID) && client.IsReferenced(zID) && client.IsReferenced(cID));
    }
    //Set default travel speed for axes
    public void SetDefaultSpeed(Vector4D vel)
    {
        DefVel = new Vector4D
        {
            X = Math.Min(vel.X, VelLimits.X),
            Y = Math.Min(vel.Y, VelLimits.Y),
            Z = Math.Min(vel.Z, VelLimits.Z),
            C = Math.Min(vel.C, VelLimits.C)
        };
    }
    //Set default travel acceleration for axes
    public void SetDefaultAcc(Vector4D vel)
    {
        DefAcc = new Vector4D
        {
            X = Math.Min(vel.X, AccLimits.X),
            Y = Math.Min(vel.Y, AccLimits.Y),
            Z = Math.Min(vel.Z, AccLimits.Z),
            C = Math.Min(vel.C, AccLimits.C)
        };
    }
    //Return default Carrier speed
    public Vector4D getDefaultSpeed()
    {
        return DefVel;
    }
    //Return default Carrier acceleration
    public Vector4D getDefaultAcc()
    {
        return DefAcc;
    }
    //Move Carrier safely (first Z axis to safety height)
    public bool SafeMove(Vector4D target, bool abs, Vector4D? vel = null)
    {
        vel ??= DefVel;
        Console.WriteLine($"SafeMove() called with target: ({target.X}, {target.Y}, {target.Z}, {target.C}), abs: {abs}, vel: {vel}");
        MoveZ(safetyHeight, true, vel.Z);
        UMoveX(target.X, true, vel.X);
        UMoveY(target.Y, true, vel.Y);
        UMoveC(target.C, true, vel.C);
        WaitMotion();
        MoveZ(target.Z, true, vel.Z);
        Console.WriteLine("Move() completed");
        return true;
    }
    //Move Carrier, beware Z axis is not in safe 
    public bool Move(Vector4D target, bool abs, Vector4D? vel = null)
    {
        vel ??= DefVel;
        Console.WriteLine($"Move() called with target: ({target.X}, {target.Y}, {target.Z}, {target.C}), abs: {abs}, vel: {vel}");
        UMoveX(target.X, true, vel.X);
        UMoveY(target.Y, true, vel.Y);
        UMoveZ(target.Z, true, vel.Z);
        UMoveC(target.C, true, vel.C);
        WaitMotion();
        Console.WriteLine("Move() completed");
        return true;
    }
    //Move Carrier beware Z axis is not in safe, return without waiting for finish
    public bool UMove(Vector4D target, bool abs, Vector4D? vel = null)
    {
        vel ??= DefVel;
        Console.WriteLine($"UMove() called with target: ({target.X}, {target.Y}, {target.Z}, {target.C}), abs: {abs}, vel: {vel}");
        UMoveX(target.X, true, vel.X);
        UMoveY(target.Y, true, vel.Y);
        UMoveZ(target.Z, true, vel.Z);
        UMoveC(target.C, true, vel.C);
        return true;
    }
    //Move Carrier X Axis, return without waiting for finish
    public void UMoveX(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.X ?? 0; // Assign 0 if DefVel or DefVel.X is null
        acc ??= DefAcc?.X ?? 0; // Assign 0 if DefAcc or DefAcc.X is null
        Console.WriteLine($"MoveX() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(xID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
    }
    //Move Carrier Y Axis, return without waiting for finish
    public void UMoveY(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.Y ?? 0;
        acc ??= DefAcc?.Y ?? 0;
        Console.WriteLine($"MoveY() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(yID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
    }
    //Move Carrier Z Axis, return without waiting for finish
    public void UMoveZ(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.Z ?? 0;
        acc ??= DefAcc?.Z ?? 0;
        Console.WriteLine($"MoveZ() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(zID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
    }
    //Move Carrier C Axis, return without waiting for finish
    public void UMoveC(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.C ?? 0;
        acc ??= DefAcc?.C ?? 0;
        Console.WriteLine($"MoveC() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(cID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
    }
    //Move Carrier X Axis
    public void MoveX(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.X ?? 0; // Assign 0 if DefVel or DefVel.X is null
        acc ??= DefAcc?.X ?? 0; // Assign 0 if DefAcc or DefAcc.X is null
        Console.WriteLine($"MoveX() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(xID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
        while (client.IsMoving(xID)) ;
    }
    //Move Carrier Y Axis
    public void MoveY(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.Y ?? 0;
        acc ??= DefAcc?.Y ?? 0;
        Console.WriteLine($"MoveY() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(yID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
        while (client.IsMoving(yID)) ;
    }
    //Move Carrier Z Axis
    public void MoveZ(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.Z ?? 0;
        acc ??= DefAcc?.Z ?? 0;
        Console.WriteLine($"MoveZ() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(zID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
        while (client.IsMoving(zID)) ;
    }
    //Move Carrier C Axis
    public void MoveC(float dest, bool abs, float? vel = null, float? acc = null)
    {
        vel ??= DefVel?.C ?? 0;
        acc ??= DefAcc?.C ?? 0;
        Console.WriteLine($"MoveC() called with dest: {dest}, abs: {abs}, vel: {vel}, acc: {acc}");
        client.Move(cID, dest, vel.GetValueOrDefault(), acc.GetValueOrDefault(), abs);
        while (client.IsMoving(cID)) ;
    }
    //Wait for current motion to finish
    public void WaitMotion()
    {
        Console.WriteLine($"Waiting for motions to finish...");
        while (client.IsMoving(xID) || client.IsMoving(yID) || client.IsMoving(zID) || client.IsMoving(cID));
        Console.WriteLine($"Motions finished...");
    }
    //Send carrier to its Home position with safe(Z) move
    public void GoHome()
    {
        SafeMove(HomePos, true);
    }
    //Send carrier to its Load position with safe(Z) move
    public void GoLoad()
    {
        SafeMove(LoadPos, true);
    }
}