using CSharpCode;

namespace CSharpPublicApiExtractor;

public static class Test
{
    public static void Start()
    {
        var code = @"
using System;

namespace ConsoleApp.PublicApiExtractor;

using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class Example
{
    public Example() { OnStatusChange?.Invoke(default); }
    string? Name { get; set; }
    public static string? Known { get; set; }
    public string? Id { get; }
    public string? Body => field;
    public string? this[int i] { get => i.ToString(); init => Name = i.ToString(); }
    public string? Height { get => field; }
    public string? Width { get => field; set => field = value; }
    public string? Top { get { return field; } set { field = value; } }
    public void DoWork() { }
    public static void DoWork(int i) { }
    public static void Do(int i = 0) { }
    public void Do<T>(T obj) where T : class { }
    public enum Status { Active, Inactive }
    public event Action<Status>? OnStatusChange;
    public delegate void Some<T>(T obj);
    public interface IMyService
    {
        void Execute();
        public void Do();
        internal void Do(int i);
        protected void Do(long i);
        public void Do(string i) { }
        public static void Do(short i) { }
        sealed class Abc : Test, IService
        {
            public Abc() : base() { }
            string IService.this[int i] => throw new NotImplementedException();
            int IService.Id => throw new NotImplementedException();
            string IService.Name => throw new NotImplementedException();
            event Action? IService.IdChanged
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }
            event Action? IService.NameChanged
            {
                add => throw new NotImplementedException();
                remove => throw new NotImplementedException();
            }
            void IService.SetId(int i) => throw new NotImplementedException();
            void IService.SetName(string i) => throw new NotImplementedException();
            public static implicit operator string(Abc abc) => throw new NotImplementedException();
            public static explicit operator Abc(string abc) => throw new NotImplementedException();
            public static Abc operator ++(Abc abc) => throw new NotImplementedException();
            public static int operator -(Abc abc) => throw new NotImplementedException();
            static explicit IId1<Abc>.operator string(Abc value) => throw new NotImplementedException();
            static Abc IId1<Abc>.operator +(Abc a, int b) => throw new NotImplementedException();
        }
        public class Test { }
        public enum Status { Active, Inactive }
        public record Parameter(int Number1, double Number2);
        public readonly record struct Record(int Number1, double Number2);
        public readonly record struct Block(int Number1, double Number2)
        {
            public void Do() { }
            public enum Status { Active, Inactive }
        }
    }
}
public interface IMyService
{
    int Number { get; set; }
    public int Count { set; }
    event EventHandler Handler;
    void Execute();
    public void Do();
    internal void Do(int i);
    protected void Do(long i);
    public void Do(string i) { }
    public static void Do(short i) { }
    class Abc { }
    public class Test { }
    public enum Status { Active, Inactive }
}
public interface IService : IId1<Example.IMyService.Abc>
{
    string this[int i] { get; }
    int IId1<Example.IMyService.Abc>.Id => throw new NotImplementedException();
    new int Id { get; }
    internal string Name { get; }
    event Action? IdChanged
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }
    internal event Action? NameChanged;
    void SetId(int i);
    internal void SetName(string i);
}
public interface IId1<TSelf> where TSelf : IId1<TSelf>
{
    int Id { get; }
    static abstract explicit operator string(TSelf value);
    static abstract TSelf operator +(TSelf a, int b);
}

public enum Status { Active, Inactive }

public delegate void Some<T>(T obj);

public struct Other { }

public readonly struct Another { }

public static class Extension
{
    extension(Example test)
    {
        void Do() => test.DoWork();
        public void Undo() => test.ToString();
    }
    public static void Redo(this Example test) { }
}
";

        var extractor = new PublicApiExtractor();
        extractor.ExtractPublicDeclarations(code, "api.txt");
        Console.WriteLine("测试完成");
    }
}
