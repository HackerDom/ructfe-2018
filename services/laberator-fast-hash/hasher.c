#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#include <string.h>

#define BLOCK_SIZE 16

char buffer[BLOCK_SIZE * 2];

void compress(char* data) {
	for (int i = 0; i < BLOCK_SIZE; ++i) {
		buffer[i] = data[i * 2] + data[i * 2 + 1];
	}
	for (int i = 0; i < BLOCK_SIZE; ++i) {
		data[i + BLOCK_SIZE] = buffer[i];
	}
}

char* prepare_data(const char* data, size_t data_length) {
	size_t new_data_length = (size_t)(ceil(data_length / ((double)(BLOCK_SIZE))) * BLOCK_SIZE);
	char* new_data = malloc(new_data_length * sizeof(char*));
	for (size_t i = 0; i < data_length; ++i) {
		new_data[i] = data[i];
	}
	for (size_t i = data_length; i < new_data_length; ++i) {
		new_data[i] = 0;
	}
	return new_data;
}

void get_hash(const char* data, size_t data_length, char* out) {
	for (size_t i = 0; i < BLOCK_SIZE * 2; ++i) {
		buffer[i] = (char)i;
	}
	char* prepared_data = prepare_data(data, data_length);
	size_t new_data_length = (size_t)(ceil(data_length / ((double)(BLOCK_SIZE))) * BLOCK_SIZE);
	for (int j = 0; j < new_data_length / BLOCK_SIZE; ++j) {
		for (int i = 0; i < BLOCK_SIZE; ++i) {
			buffer[i] = prepared_data[i + j * BLOCK_SIZE];
		}
		compress(buffer);
	}
	for (int i = 0; i < BLOCK_SIZE; ++i) {
		out[i] = buffer[i + BLOCK_SIZE];
	}
	free(prepared_data);
}
