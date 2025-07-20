namespace Application.Exceptions;

public class NotFoundException<T>(T key) : Exception($"Entity '{typeof(T).Name}' ({key}) was not found.");