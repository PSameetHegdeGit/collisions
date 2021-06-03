## Collision Detection of Prisms in Unity with GJK Algorithm and EPA Algorithm

This project randomly generates 2-dimensional prisms on a 2-dimensional space in random locations in Unity. Some of these prisms will be overlapping while others are not overlapping.

The project uses the GJK algorithm to detect if prisms are overlapping. 

Below is information on the GJK Algorithm: 
(https://en.wikipedia.org/wiki/Gilbert%E2%80%93Johnson%E2%80%93Keerthi_distance_algorithm)

Of those prisms that are detected to be colliding, we use the EPA algorithm to separate the prisms in as optimal a manner as possible.

Below is information on the EPA Algorithm:
(https://dyn4j.org/2010/05/epa-expanding-polytope-algorithm/#:~:text=EPA%20stands%20for%20Expanding%20Polytope,the%20origin%20of%20the%20polytope.)
