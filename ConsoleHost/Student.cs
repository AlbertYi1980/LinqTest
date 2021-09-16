namespace ConsoleHost
{
    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public bool Enabled { get; set; }
        
        public int Age { get; set; }

        public override string ToString()
        {
            return $"id:{Id},name:{Name}";
        }
    }

 
}